using System.Text;
using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Renci.SshNet;

namespace Medley.Application.Services;

/// <summary>
/// Service for interacting with Cursor CLI via SSH using SSH.NET
/// </summary>
public class CursorService : ICursorService
{
    private readonly CursorSettings _settings;
    private readonly ILogger<CursorService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<AiPrompt> _promptRepository;

    public CursorService(
        IOptions<CursorSettings> settings,
        ILogger<CursorService> logger,
        IUnitOfWork unitOfWork,
        IRepository<AiPrompt> promptRepository)
    {
        _settings = settings.Value;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _promptRepository = promptRepository;
    }

    /// <inheritdoc />
    public async Task<CursorReviewResult> ReviewArticleAsync(
        string articleContent,
        string instructions,
        Guid? articleTypeId = null,
        CancellationToken cancellationToken = default)
    {
        var remoteFileName = $"article_{Guid.NewGuid():N}.md";
        // Place articles in medley-tmp subfolder within the workspace
        var remotePath = $"{_settings.WorkspaceDirectory}/medley-tmp/{remoteFileName}";

        // Create SSH client with key-based authentication
        using var client = CreateSshClient();

        try
        {
            _logger.LogInformation("Connecting to SSH server: {Host}:{Port}", _settings.SshHost, _settings.SshPort);

            await client.ConnectAsync(cancellationToken);
            
            if (!client.IsConnected)
            {
                throw new InvalidOperationException("Failed to connect to SSH server");
            }

            _logger.LogInformation("Connected to SSH server successfully");

            // Sync CursorReview prompt (global instructions for Cursor)
            var cursorReviewPromptPath = await SyncCursorReviewPromptAsync(client, cancellationToken);

            // Sync article type prompt if articleTypeId is provided
            string? articleTypePromptPath = null;
            if (articleTypeId.HasValue)
            {
                articleTypePromptPath = await SyncArticleTypePromptAsync(
                    client, 
                    articleTypeId.Value, 
                    cancellationToken);
            }

            // Step 1: Write article content to remote file
            await WriteRemoteFileAsync(client, remotePath, articleContent, cancellationToken);
            _logger.LogInformation("Article content written to remote file: {RemotePath}", remotePath);

            // Step 2: Execute Cursor CLI
            var (cursorCommand, stdinInput) = BuildArticleReviewCommand(
                remoteFileName, 
                instructions,
                cursorReviewPromptPath,
                articleTypePromptPath);
            var (exitCode, stdout, stderr) = await ExecuteCommandAsync(client, cursorCommand, cancellationToken, stdinInput);

            if (exitCode != 0)
            {
                _logger.LogError("Cursor execution failed with exit code {ExitCode}. Error: {Error}", exitCode, stderr);
                return new CursorReviewResult
                {
                    Success = false,
                    Error = $"Cursor execution failed: {stderr}"
                };
            }

            // Step 3: Use Cursor's text output as changes summary
            var changesSummary = string.IsNullOrWhiteSpace(stdout)
                ? "Article improved by Cursor Agent"
                : stdout.Trim();
            _logger.LogInformation("Cursor execution completed successfully");

            // Step 4: Read improved content from remote file
            var improvedContent = await ReadRemoteFileAsync(client, remotePath, cancellationToken);
            _logger.LogInformation("Read improved content ({Length} characters)", improvedContent.Length);

            return new CursorReviewResult
            {
                Success = true,
                ImprovedContent = improvedContent,
                ChangesSummary = changesSummary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Cursor review");
            return new CursorReviewResult
            {
                Success = false,
                Error = ex.Message
            };
        }
        finally
        {
            // Step 5: Cleanup - delete remote file
            try
            {
                if (client.IsConnected)
                {
                    await DeleteRemoteFileAsync(client, remotePath, cancellationToken);
                    _logger.LogInformation("Cleaned up remote file: {RemotePath}", remotePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup remote file: {RemotePath}", remotePath);
            }

            if (client.IsConnected)
            {
                client.Disconnect();
            }
        }
    }

    /// <summary>
    /// Creates an SSH client with the configured settings
    /// </summary>
    private SshClient CreateSshClient()
    {
        var authMethod = new PrivateKeyAuthenticationMethod(
            _settings.SshUser,
            new PrivateKeyFile(_settings.SshKeyPath));

        var connectionInfo = new ConnectionInfo(
            _settings.SshHost,
            _settings.SshPort,
            _settings.SshUser,
            authMethod)
        {
            Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds)
        };

        return new SshClient(connectionInfo);
    }

    /// <summary>
    /// Creates an SFTP client with the configured settings
    /// </summary>
    private SftpClient CreateSftpClient()
    {
        var authMethod = new PrivateKeyAuthenticationMethod(
            _settings.SshUser,
            new PrivateKeyFile(_settings.SshKeyPath));

        var connectionInfo = new ConnectionInfo(
            _settings.SshHost,
            _settings.SshPort,
            _settings.SshUser,
            authMethod)
        {
            Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds)
        };

        return new SftpClient(connectionInfo);
    }

    /// <summary>
    /// Executes an SSH command on the remote server
    /// </summary>
    /// <param name="stdinInput">Optional input to stream to the command's stdin</param>
    private async Task<(int exitCode, string stdout, string stderr)> ExecuteCommandAsync(
        SshClient client,
        string command,
        CancellationToken cancellationToken,
        string? stdinInput = null)
    {
        using var sshCommand = client.CreateCommand(command);
        sshCommand.CommandTimeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        
        // Start command execution asynchronously
        var executeTask = sshCommand.ExecuteAsync(cancellationToken);
        
        // If stdin input is provided, stream it to the command
        if (!string.IsNullOrEmpty(stdinInput))
        {
            using var inputStream = sshCommand.CreateInputStream();
            var inputBytes = Encoding.UTF8.GetBytes(stdinInput);
            await inputStream.WriteAsync(inputBytes, 0, inputBytes.Length, cancellationToken);
            await inputStream.FlushAsync(cancellationToken);
            // Close the input stream to signal EOF
            inputStream.Close();
        }
        
        // Wait for command execution to complete
        await executeTask;
        
        var exitCode = sshCommand.ExitStatus ?? -1;
        var stdout = sshCommand.Result ?? string.Empty;
        var stderr = sshCommand.Error ?? string.Empty;
        
        _logger.LogDebug("Command executed with exit code {ExitCode}", exitCode);
        
        return (exitCode, stdout, stderr);
    }

    /// <summary>
    /// Writes content to a remote file via SFTP (reliable for file transfers)
    /// </summary>
    private async Task WriteRemoteFileAsync(
        SshClient sshClient,
        string remotePath,
        string content,
        CancellationToken cancellationToken)
    {
        // Use SFTP for reliable file uploads - it's designed for this purpose
        using var sftpClient = CreateSftpClient();

        await sftpClient.ConnectAsync(cancellationToken);

        try
        {
            var contentBytes = Encoding.UTF8.GetBytes(content);
            using var contentStream = new MemoryStream(contentBytes);

            // Ensure the directory exists
            var directory = Path.GetDirectoryName(remotePath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(directory) && !sftpClient.Exists(directory))
            {
                await sftpClient.CreateDirectoryAsync(directory, cancellationToken);
            }

            // Upload the file
            await sftpClient.UploadFileAsync(contentStream, remotePath, cancellationToken);
        }
        finally
        {
            if (sftpClient.IsConnected)
            {
                sftpClient.Disconnect();
            }
        }
    }

    /// <summary>
    /// Reads content from a remote file via SFTP (reliable for file transfers)
    /// </summary>
    private async Task<string> ReadRemoteFileAsync(
        SshClient sshClient,
        string remotePath,
        CancellationToken cancellationToken)
    {
        // Use SFTP for reliable file downloads - it's designed for this purpose
        using var sftpClient = CreateSftpClient();
        
        await sftpClient.ConnectAsync(cancellationToken);
            
        try
        {
            using var contentStream = new MemoryStream();
            await sftpClient.DownloadFileAsync(remotePath, contentStream, cancellationToken);
                
            contentStream.Position = 0;
            using var reader = new StreamReader(contentStream, Encoding.UTF8);
            return await reader.ReadToEndAsync(cancellationToken);
        }
        finally
        {
            if (sftpClient.IsConnected)
            {
                sftpClient.Disconnect();
            }
        }
    }

    /// <summary>
    /// Deletes a remote file via SSH
    /// </summary>
    private async Task DeleteRemoteFileAsync(
        SshClient client,
        string remotePath,
        CancellationToken cancellationToken)
    {
        var command = $"rm {remotePath}";
        await ExecuteCommandAsync(client, command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CursorQuestionResult> AskQuestionAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateSshClient();

        try
        {
            _logger.LogInformation("Connecting to SSH server for question: {Host}:{Port}", _settings.SshHost, _settings.SshPort);

            await client.ConnectAsync(cancellationToken);
            
            if (!client.IsConnected)
            {
                throw new InvalidOperationException("Failed to connect to SSH server");
            }

            _logger.LogInformation("Connected to SSH server successfully");

            // Build command - no file operations needed
            var (command, stdinInput) = BuildQuestionCommand(question);
            
            // Execute command
            var (exitCode, stdout, stderr) = await ExecuteCommandAsync(client, command, cancellationToken, stdinInput);

            if (exitCode != 0)
            {
                _logger.LogError("Question failed with exit code {ExitCode}. Error: {Error}", exitCode, stderr);
                return new CursorQuestionResult
                {
                    Success = false,
                    Error = $"Command execution failed: {stderr}"
                };
            }

            var response = string.IsNullOrWhiteSpace(stdout)
                ? "No response received from Cursor Agent"
                : stdout.Trim();

            _logger.LogInformation("Question completed successfully");

            return new CursorQuestionResult
            {
                Success = true,
                Response = response
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during question");
            return new CursorQuestionResult
            {
                Success = false,
                Error = ex.Message
            };
        }
        finally
        {
            if (client.IsConnected)
            {
                client.Disconnect();
            }
        }
    }

    /// <summary>
    /// Syncs a prompt to the remote Cursor workspace if needed
    /// </summary>
    /// <param name="client">Connected SSH client</param>
    /// <param name="prompt">The prompt to sync</param>
    /// <param name="relativeFilePath">The relative file path where the prompt should be synced</param>
    /// <param name="promptDescription">Description of the prompt for logging purposes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The relative path to the synced prompt file</returns>
    private async Task<string> SyncPromptAsync(
        SshClient client,
        AiPrompt prompt,
        string relativeFilePath,
        string promptDescription,
        CancellationToken cancellationToken)
    {
        // Determine if sync is needed
        var needsSync = prompt.LastSyncedWithCursor == null || 
                       (prompt.LastModifiedAt.HasValue && 
                        prompt.LastSyncedWithCursor < prompt.LastModifiedAt);

        var fullPromptPath = $"{_settings.WorkspaceDirectory}{relativeFilePath}";

        if (needsSync)
        {
            _logger.LogInformation(
                "Syncing {PromptDescription} to {Path}", 
                promptDescription, 
                fullPromptPath);

            // Write the prompt content to remote file
            await WriteRemoteFileAsync(client, fullPromptPath, prompt.Content, cancellationToken);

            // Update LastSyncedWithCursor timestamp
            prompt.LastSyncedWithCursor = DateTimeOffset.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully synced {PromptDescription}", 
                promptDescription);
        }
        else
        {
            _logger.LogInformation(
                "{PromptDescription} is already up to date (last synced: {LastSynced})",
                promptDescription,
                prompt.LastSyncedWithCursor);
        }

        return relativeFilePath;
    }

    /// <summary>
    /// Syncs an article type prompt to the remote Cursor workspace if needed
    /// </summary>
    /// <param name="client">Connected SSH client</param>
    /// <param name="articleTypeId">The article type ID to lookup and sync the prompt for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The relative path to the synced prompt file, or null if no prompt found</returns>
    private async Task<string?> SyncArticleTypePromptAsync(
        SshClient client,
        Guid articleTypeId,
        CancellationToken cancellationToken)
    {
        // Lookup the article type prompt
        var articleTypePrompt = await _promptRepository
            .Query()
            .Include(p => p.ArticleType)
            .FirstOrDefaultAsync(
                p => p.Type == PromptType.ArticleTypeAgentMode && 
                     p.ArticleTypeId == articleTypeId,
                cancellationToken);

        if (articleTypePrompt == null)
        {
            _logger.LogWarning(
                "No ArticleTypeAgentMode prompt found for ArticleTypeId: {ArticleTypeId}", 
                articleTypeId);
            return null;
        }

        if (articleTypePrompt.ArticleType == null)
        {
            throw new InvalidOperationException("ArticleType must be loaded for syncing");
        }

        // Sanitize article type name for file system
        var sanitizedName = SanitizeFileName(articleTypePrompt.ArticleType.Name);
        var relativePromptPath = $"/medley-tmp/article-types/{sanitizedName}.md";
        var promptDescription = $"article type prompt '{articleTypePrompt.ArticleType.Name}'";

        return await SyncPromptAsync(client, articleTypePrompt, relativePromptPath, promptDescription, cancellationToken);
    }

    /// <summary>
    /// Syncs the CursorReview prompt to the remote Cursor workspace if needed
    /// </summary>
    /// <param name="client">Connected SSH client</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The relative path to the synced prompt file</returns>
    private async Task<string> SyncCursorReviewPromptAsync(
        SshClient client,
        CancellationToken cancellationToken)
    {
        // Lookup the CursorReview prompt
        var cursorReviewPrompt = await _promptRepository
            .Query()
            .FirstOrDefaultAsync(
                p => p.Type == PromptType.CursorReview,
                cancellationToken);

        if (cursorReviewPrompt == null)
        {
            throw new InvalidOperationException("CursorReview prompt not found. Please create one in the system.");
        }

        var relativePromptPath = "/medley-tmp/cursor-review.md";
        var promptDescription = "CursorReview prompt";

        return await SyncPromptAsync(client, cursorReviewPrompt, relativePromptPath, promptDescription, cancellationToken);
    }

    /// <summary>
    /// Sanitizes a file name by removing or replacing invalid characters
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Trim();
    }

    /// <summary>
    /// Builds the Cursor CLI command and stdin input
    /// </summary>
    /// <param name="remoteFileName">The article file name</param>
    /// <param name="instructions">Review instructions</param>
    /// <param name="cursorReviewPromptPath">Path to the CursorReview prompt file</param>
    /// <param name="articleTypePromptPath">Optional path to article type prompt file</param>
    /// <returns>Tuple of (command, stdinInput)</returns>
    private (string command, string stdinInput) BuildArticleReviewCommand(
        string remoteFileName, 
        string instructions,
        string cursorReviewPromptPath,
        string? articleTypePromptPath = null)
    {
        // Build the command - execute from workspace root
        // Use -p (print mode) with --force to modify files
        // Default text format gives clean, final-answer-only responses
        var command = $"cd {_settings.WorkspaceDirectory} && {_settings.AgentPath} --model {_settings.Model} -p --force";

        // The instructions/prompt and file reference are passed via stdin to avoid command line parsing issues
        var stdinInput = $"Improve this article based on: {instructions}. File: /medley-tmp/{remoteFileName}";
        
        // Reference the CursorReview prompt (general instructions)
        stdinInput += $"\n\nGeneral review instructions can be found in: {cursorReviewPromptPath}";
        
        // If article type prompt is provided, reference it as additional directions
        if (!string.IsNullOrEmpty(articleTypePromptPath))
        {
            stdinInput += $"\n\nAdditional directions for this article type can be found in: {articleTypePromptPath}";
        }

        return (command, stdinInput);
    }

    /// <summary>
    /// Builds a question command for Cursor CLI
    /// </summary>
    /// <returns>Tuple of (command, stdinInput)</returns>
    private (string command, string stdinInput) BuildQuestionCommand(string question)
    {
        // Build the command - execute from workspace root
        // Use -p (print mode) for text output only, no file modifications
        var command = $"cd {_settings.WorkspaceDirectory} && {_settings.AgentPath} --model {_settings.Model} -p";
        
        // The question is passed via stdin to avoid command line parsing issues
        var stdinInput = question;
        
        return (command, stdinInput);
    }
}
