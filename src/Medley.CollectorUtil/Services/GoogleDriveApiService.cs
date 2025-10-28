using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Medley.CollectorUtil.Data;
using Medley.CollectorUtil.Models;

namespace Medley.CollectorUtil.Services;

/// <summary>
/// Service for listing Google Meet videos from Google Drive using the Drive API
/// </summary>
public class GoogleDriveApiService
{
    private readonly GoogleAuthService _googleAuthService;
    private readonly ConfigurationService _configurationService;

    public GoogleDriveApiService(GoogleAuthService googleAuthService, ConfigurationService configurationService)
    {
        _googleAuthService = googleAuthService;
        _configurationService = configurationService;
    }

    /// <summary>
    /// Lists Google Meet videos from Google Drive
    /// </summary>
    public async Task<List<DriveVideo>> ListGoogleMeetVideosAsync(CancellationToken cancellationToken = default)
    {
        // Get Google OAuth credential
        var credential = await _googleAuthService.GetCredentialAsync();
        if (credential == null)
        {
            throw new InvalidOperationException("Not authenticated with Google. Please authenticate first using OAuth Flow.");
        }

        // Create Drive API service
        var driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Medley Collector"
        });

        // Step 1: Build folder hierarchy
        var folderHierarchy = await BuildFolderHierarchyAsync(driveService, cancellationToken);

        // Step 2: Get all videos (no folder filter in query)
        var allVideos = await ListVideosInQueryAsync(driveService, "mimeType='video/mp4'", folderHierarchy, cancellationToken);

        // Step 3: Filter programmatically if folder ID is specified
        var folderId = await _configurationService.GetGoogleDriveFolderIdAsync();
        if (!string.IsNullOrWhiteSpace(folderId))
        {
            // Get all descendant folder IDs recursively
            var descendantFolderIds = GetDescendantFolderIds(folderId, folderHierarchy);
            descendantFolderIds.Add(folderId); // Include the folder itself

            // Filter videos to only those in the specified folder or its descendants
            allVideos = allVideos.Where(v => descendantFolderIds.Contains(v.ParentFolderId)).ToList();
        }

        return allVideos;
    }

    /// <summary>
    /// Builds a hierarchical map of all folders
    /// </summary>
    private async Task<Dictionary<string, FolderInfo>> BuildFolderHierarchyAsync(
        DriveService driveService,
        CancellationToken cancellationToken)
    {
        var folderHierarchy = new Dictionary<string, FolderInfo>();

        var request = driveService.Files.List();
        request.Q = "mimeType='application/vnd.google-apps.folder' and trashed=false";
        request.Fields = "nextPageToken, files(id, name, parents)";
        request.PageSize = 1000;
        request.SupportsAllDrives = true;
        request.IncludeItemsFromAllDrives = true;
        request.Corpora = "allDrives";

        do
        {
            var result = await request.ExecuteAsync(cancellationToken);

            foreach (var folder in result.Files)
            {
                var parentId = folder.Parents?.FirstOrDefault(); // Assume only one parent
                folderHierarchy[folder.Id] = new FolderInfo
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    ParentId = parentId
                };
            }

            request.PageToken = result.NextPageToken;
        }
        while (!string.IsNullOrEmpty(request.PageToken) && !cancellationToken.IsCancellationRequested);

        return folderHierarchy;
    }

    /// <summary>
    /// Gets all descendant folder IDs recursively
    /// </summary>
    private HashSet<string> GetDescendantFolderIds(string folderId, Dictionary<string, FolderInfo> folderHierarchy)
    {
        var descendants = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(folderId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            // Find all children of the current folder
            foreach (var folder in folderHierarchy.Values)
            {
                if (folder.ParentId == currentId && !descendants.Contains(folder.Id))
                {
                    descendants.Add(folder.Id);
                    queue.Enqueue(folder.Id);
                }
            }
        }

        return descendants;
    }

    /// <summary>
    /// Gets the full folder path as an array for a folder ID
    /// </summary>
    private string[] GetFolderPathArray(string folderId, Dictionary<string, FolderInfo> folderHierarchy)
    {
        var pathParts = new List<string>();
        var currentId = folderId;

        while (!string.IsNullOrEmpty(currentId) && folderHierarchy.TryGetValue(currentId, out var folder))
        {
            pathParts.Insert(0, folder.Name);
            currentId = folder.ParentId;
        }

        return pathParts.ToArray();
    }

    /// <summary>
    /// Lists videos matching a query
    /// </summary>
    private async Task<List<DriveVideo>> ListVideosInQueryAsync(
        DriveService driveService,
        string query,
        Dictionary<string, FolderInfo> folderHierarchy,
        CancellationToken cancellationToken)
    {
        var videos = new List<DriveVideo>();

        var request = driveService.Files.List();
        request.Q = query;
        request.Fields = "nextPageToken, files(id, name, createdTime, parents, lastModifyingUser(displayName, emailAddress), videoMediaMetadata(durationMillis))";
        request.PageSize = 100;
        request.SupportsAllDrives = true;
        request.IncludeItemsFromAllDrives = true;
        request.Corpora = "allDrives";

        do
        {
            var result = await request.ExecuteAsync(cancellationToken);

            foreach (var file in result.Files)
            {
                // Assume only one parent - take the first one
                var parentId = file.Parents?.FirstOrDefault();
                var folderPathArray = Array.Empty<string>();
                
                if (!string.IsNullOrEmpty(parentId))
                {
                    // Get full folder path using hierarchy
                    folderPathArray = GetFolderPathArray(parentId, folderHierarchy);
                }

                videos.Add(new DriveVideo
                {
                    Id = file.Id,
                    Name = file.Name,
                    CreatedTime = file.CreatedTime,
                    ParentFolderId = parentId ?? string.Empty,
                    FolderPath = folderPathArray,
                    LastModifyingUserName = file.LastModifyingUser?.DisplayName ?? string.Empty,
                    LastModifyingUserEmail = file.LastModifyingUser?.EmailAddress ?? string.Empty,
                    DurationMillis = file.VideoMediaMetadata?.DurationMillis
                });
            }

            request.PageToken = result.NextPageToken;
        }
        while (!string.IsNullOrEmpty(request.PageToken) && !cancellationToken.IsCancellationRequested);

        return videos;
    }
}

