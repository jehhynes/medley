# Cursor CLI Integration Setup Guide

## Overview

The Medley application now includes integration with Cursor CLI running on a Linux server. This allows the LLM assistant to leverage Cursor Agent for code-aware article improvements via SSH.

## Architecture

```
Windows Server (Medley App)
    ↓ SSH
Linux Server (Cursor CLI)
    ↓ Executes
Cursor Agent (AI improvements)
```

## Components Created

### 1. Configuration
- **CursorSettings.cs** - Configuration class for SSH and Cursor settings
- **appsettings.json** - Added Cursor section with default disabled state

### 2. Service Layer
- **ICursorService** - Interface for Cursor operations
- **CursorService** - Implementation using **SSH.NET** library for SSH operations and Cursor CLI execution
- **CursorReviewResult** - Result class containing improved content and change summary

### 3. Dependencies
- **SSH.NET (v2025.1.0)** - Industry-standard .NET SSH library (227.7M downloads)

### 4. Tool Integration
- **ArticleChatTools.ReviewArticleWithCursorAsync()** - LLM tool method for article reviews
- **ArticleChatTools.AskQuestionWithCursorAsync()** - LLM tool method for asking questions
- Updated ArticleChatToolsFactory to inject CursorService
- Updated ArticleChatService to register the tools when enabled

## Linux Server Setup

### Prerequisites
1. Ubuntu 22.04 or later
2. SSH server installed and running (with SFTP enabled - enabled by default)
3. Cursor CLI installed
4. Windows Server does **NOT** need OpenSSH client - SSH.NET handles everything!

### Installation Steps

```bash
# 1. Install Cursor CLI
curl https://cursor.com/install -fsS | bash

# 2. Create workspace directory and temp subfolder
mkdir -p ~/workspace/medley-tmp
chmod 755 ~/workspace
chmod 777 ~/workspace/medley-tmp

# 3. Add Windows Server SSH public key
# Copy the public key from Windows Server to ~/.ssh/authorized_keys
# Or generate on Linux and add to Windows if using password auth initially:
ssh-keygen -t rsa -b 4096 -C "medley@cursor"
# Then copy the public key to Windows Server

# 4. Test Cursor installation
agent --version

# 5. Test SSH connection from Windows Server
# From Windows PowerShell:
ssh -i C:\Keys\medley-cursor.pem medley@cursor.internal.yourcompany.com "agent --version"
```

## Windows Server Configuration

### 1. Generate SSH Key (if not already done)

You can generate the SSH key on either Windows or Linux:

**Option A: Generate on Linux (recommended):**
```bash
ssh-keygen -t rsa -b 4096 -f ~/.ssh/medley-cursor
# Copy the private key to Windows: C:\Keys\medley-cursor.pem
# Add public key to ~/.ssh/authorized_keys
```

**Option B: Generate on Windows:**
```powershell
# Requires OpenSSH client (or use PuTTYgen)
ssh-keygen -t rsa -b 4096 -f C:\Keys\medley-cursor.pem

# Set proper permissions (Windows)
icacls C:\Keys\medley-cursor.pem /inheritance:r
icacls C:\Keys\medley-cursor.pem /grant:r "%USERNAME%:R"
```

### 2. Configure appsettings.json

Update `src/Medley.Web/appsettings.json`:

```json
{
  "Cursor": {
    "Enabled": true,
    "SshHost": "cursor.internal.yourcompany.com",
    "SshUser": "medley",
    "SshKeyPath": "C:\\Keys\\medley-cursor.pem",
    "SshPort": 22,
    "WorkspaceDirectory": "/home/medley/workspace",
    "AgentPath": "/home/medley/.local/bin/agent",
    "TimeoutSeconds": 300,
    "Model": "claude-4.5-sonnet-thinking"
  }
}
```

### Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `Enabled` | Enable/disable Cursor integration | `false` |
| `SshHost` | Linux server hostname or IP | (required) |
| `SshUser` | SSH username | `medley` |
| `SshKeyPath` | Path to SSH private key | (required) |
| `SshPort` | SSH port | `22` |
| `WorkspaceDirectory` | Cursor workspace directory (entrypoint) | `/home/medley/workspace` |
| `AgentPath` | Path to Cursor agent command | `agent` |
| `TimeoutSeconds` | Timeout for Cursor operations | `300` (5 minutes) |
| `Model` | AI model to use | `claude-4.5-sonnet-thinking` |

**Note:** Articles are automatically placed in a `medley-tmp` subfolder within the workspace directory.

## How It Works

### Article Review Flow

1. **LLM calls tool**: `ReviewArticleWithCursorAsync(instructions)`
2. **Load article**: Article content loaded from database
3. **Write to remote**: Content written to Linux server via SFTP
4. **Execute Cursor**: Cursor CLI processes the file with AI improvements using specified model
5. **Read improved content**: Modified file read back via SFTP
6. **Create version**: New ArticleVersion created with improved content
7. **Cleanup**: Remote file deleted
8. **Return result**: Improved content and change summary returned to LLM

### Question Flow

1. **LLM calls tool**: `AskQuestionWithCursorAsync(question)`
2. **Execute Cursor**: Cursor CLI answers question using specified model
3. **Return result**: Response returned to LLM (no file operations)

### SSH.NET Implementation

The service uses the **SSH.NET** library (Renci.SshNet) with the proper protocols:

**SSH for Command Execution:**
```csharp
var sshClient = new SshClient(host, port, username, new PrivateKeyFile(keyPath));
sshClient.Connect();
var command = sshClient.CreateCommand("agent -p --force ...");
var result = command.Execute();
```

**SFTP for File Operations:**
```csharp
var sftpClient = new SftpClient(host, port, username, new PrivateKeyFile(keyPath));
sftpClient.Connect();

// Upload file
using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
sftpClient.UploadFile(stream, "/path/to/file", true);

// Download file
using var downloadStream = new MemoryStream();
sftpClient.DownloadFile("/path/to/file", downloadStream);
```

**Why SFTP for files?**
- ✅ Designed specifically for file transfers
- ✅ Handles large files reliably
- ✅ No stdin/stdout complexity
- ✅ Proper binary handling
- ✅ No shell escaping issues

**Model Selection:**
The service now supports specifying which AI model to use via the `Model` configuration:
- `claude-4.5-sonnet-thinking` - Claude Sonnet 4.5 (recommended)
- Other models supported by Cursor CLI

No need for external SSH executables or complex process management!

## Testing

### 1. Test SSH Connection (Optional - if you have OpenSSH)

```powershell
# From Windows Server (if OpenSSH is installed)
ssh -i C:\Keys\medley-cursor.pem medley@cursor.internal.yourcompany.com "echo 'Connection successful'"
```

**Note:** SSH.NET doesn't require OpenSSH on Windows, but having it helps with testing!

### 2. Test Cursor CLI from Linux

```bash
# SSH to Linux server
ssh medley@cursor.internal.yourcompany.com

# Test Cursor
agent --version

# Test in workspace directory
cd ~/workspace/medley-tmp
echo "Test content" > test.md
cd ~/workspace
agent -p "Improve this" medley-tmp/test.md
cat medley-tmp/test.md
rm medley-tmp/test.md
```

### 3. Test via Application

The best way to test is through the application itself:

### 3a. Enable in Application

1. Set `"Enabled": true` in appsettings.json
2. Restart the Medley application
3. Start a chat with an article
4. Ask the LLM to review the article using Cursor
5. Check logs for execution details

## Troubleshooting

### SSH Connection Issues

**Problem:** "Permission denied (publickey)"
- Verify SSH key path is correct in appsettings.json
- Check key file exists at the specified path
- Verify public key is in `~/.ssh/authorized_keys` on Linux server
- Check key format - SSH.NET supports OpenSSH, PEM, and PuTTY formats

**Problem:** "Connection timed out"
- Verify SSH port is correct (default: 22)
- Check firewall rules between Windows and Linux servers
- Verify Linux server SSH service is running: `sudo systemctl status sshd`
- Try increasing `TimeoutSeconds` in configuration

### Cursor Execution Issues

**Problem:** "Command not found: agent"
- Verify Cursor CLI is installed on Linux server
- Check if `agent` is in PATH
- Try full path: `/usr/local/bin/agent`

**Problem:** "Cursor execution timed out"
- Increase `TimeoutSeconds` in configuration
- Check Linux server resources (CPU, memory)
- Review Cursor logs on Linux server

### File Operation Issues

**Problem:** "Failed to write remote file"
- Verify workspace directory exists: `~/workspace/medley-tmp`
- Check directory is writable: `ls -la ~/workspace/`
- Check disk space on Linux server: `df -h`
- Ensure medley-tmp subfolder has write permissions: `chmod 777 ~/workspace/medley-tmp`

## Security Considerations

1. **SSH Key Security**
   - Store private key with restrictive permissions
   - Use separate key for Cursor integration (not reused)
   - Rotate keys periodically

2. **Network Security**
   - Use private network or VPN for SSH connections
   - Configure firewall to allow only Medley server
   - Consider using SSH certificates instead of keys

3. **File Isolation**
   - Each article gets unique filename with GUID
   - Files deleted immediately after use
   - Articles isolated in `medley-tmp` subfolder
   - Workspace directory is the Cursor entrypoint

4. **Error Handling**
   - Sensitive information not logged
   - Cleanup guaranteed via finally blocks
   - Timeouts prevent hung connections

## Monitoring

### Logs to Watch

- **Application logs**: `logs/medley-*.txt`
- **SSH operations**: Look for "Cursor review" and "SSH" log entries
- **Linux server logs**: `/var/log/auth.log` for SSH connections

### Key Metrics

- Cursor execution time (typically 30-120 seconds)
- SSH connection failures
- File cleanup success rate
- Concurrent Cursor operations (max recommended: 10)

## Cost Considerations

### Infrastructure
- Linux server: ~$10-20/month (t3.small EC2 instance)
- No additional cost for Cursor CLI (uses your Cursor subscription)
- Minimal data transfer costs

### Optimization
- Single Linux server sufficient for 2-3 concurrent users
- Can scale horizontally by adding more servers if needed
- Consider spot instances for development/testing

## Future Enhancements

1. **Connection Pooling**: Reuse SSH connections for multiple operations
2. **Load Balancing**: Support multiple Linux servers
3. **Monitoring Dashboard**: Real-time status of Cursor operations
4. **Retry Logic**: Automatic retry on transient failures
5. **Caching**: Cache Cursor results for identical requests

## References

- [Cursor CLI Documentation](https://cursor.com/docs/cli/headless)
- [OpenSSH Windows Documentation](https://docs.microsoft.com/en-us/windows-server/administration/openssh/openssh_overview)
- [AWS EC2 Instance Types](https://aws.amazon.com/ec2/instance-types/)
