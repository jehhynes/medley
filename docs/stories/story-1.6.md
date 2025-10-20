# Story 1.6: AWS Integration Setup

Status: Ready

## Story

As a developer,
I want to configure AWS services for cloud storage and AI processing,
So that the platform can scale and integrate with Claude 4.5 via Bedrock.

## Acceptance Criteria

1. AWS SDK configured for .NET application with proper dependency injection
2. AWS S3 bucket setup for document storage and file uploads
3. AWS Bedrock client configuration for Claude 4.5 access
4. AWS credentials management and security configuration
5. Storage service abstraction layer implemented for file operations
6. Basic AWS service health checks implemented

## Tasks / Subtasks

- [ ] Task 1: Configure AWS SDK and Credentials (AC: 1, 4)
  - [ ] Subtask 1.1: Install AWS SDK NuGet packages (AWSSDK.Core, AWSSDK.S3, AWSSDK.BedrockRuntime)
  - [ ] Subtask 1.2: Create strongly-typed configuration classes (AwsSettings, S3Settings, BedrockSettings, FileStorageSettings)
  - [ ] Subtask 1.3: Add FileStorageProvider enum (Local, S3) and provider selection configuration
  - [ ] Subtask 1.4: Configure AWS credentials using appsettings.json for development
  - [ ] Subtask 1.5: Set up AWS credentials provider with environment variable support
  - [ ] Subtask 1.6: Configure AWS region settings and service endpoints
  - [ ] Subtask 1.7: Register strongly-typed configuration classes using IOptions pattern
  - [ ] Subtask 1.8: Register AWS services in dependency injection container with conditional registration

- [ ] Task 2: Implement File Storage Service Abstraction (AC: 5)
  - [ ] Subtask 2.1: Create IFileStorageService interface in Application layer
  - [ ] Subtask 2.2: Implement LocalFileStorageService in Infrastructure layer for development
  - [ ] Subtask 2.3: Implement S3FileStorageService in Infrastructure layer for production
  - [ ] Subtask 2.4: Add file upload functionality with content type handling (both implementations)
  - [ ] Subtask 2.5: Implement file download with stream support (both implementations)
  - [ ] Subtask 2.6: Add file deletion and existence checking (both implementations)
  - [ ] Subtask 2.7: Implement pre-signed URL generation (S3) and temporary URL generation (Local)
  - [ ] Subtask 2.8: Add provider selection logic based on configuration setting

- [ ] Task 3: Configure S3 Bucket and Storage (AC: 2)
  - [ ] Subtask 3.1: Add S3 configuration to appsettings.json (bucket name, region, folder structure)
  - [ ] Subtask 3.2: Bind S3Settings configuration class using IOptions<S3Settings>
  - [ ] Subtask 3.3: Implement bucket initialization and validation
  - [ ] Subtask 3.4: Configure S3 bucket policies for application access
  - [ ] Subtask 3.5: Set up folder structure conventions for organized storage
  - [ ] Subtask 3.6: Configure S3 lifecycle policies for cost optimization

- [ ] Task 4: Configure AWS Bedrock Client (AC: 3)
  - [ ] Subtask 4.1: Install AWSSDK.BedrockRuntime NuGet package
  - [ ] Subtask 4.2: Add Bedrock configuration to appsettings.json (model ID, region, parameters)
  - [ ] Subtask 4.3: Bind BedrockSettings configuration class using IOptions<BedrockSettings>
  - [ ] Subtask 4.4: Create IAiProcessingService interface in Application layer
  - [ ] Subtask 4.5: Implement BedrockAiService in Infrastructure layer with injected IOptions<BedrockSettings>
  - [ ] Subtask 4.6: Configure Claude 4.5 model ID and parameters from settings
  - [ ] Subtask 4.7: Implement basic prompt invocation with error handling
  - [ ] Subtask 4.8: Add response parsing and validation

- [ ] Task 5: Implement AWS Health Checks (AC: 6)
  - [ ] Subtask 5.1: Create S3 health check to verify bucket access
  - [ ] Subtask 5.2: Create Bedrock health check to verify API connectivity
  - [ ] Subtask 5.3: Register health checks in ASP.NET Core health check system
  - [ ] Subtask 5.4: Add health check endpoints to monitoring dashboard
  - [ ] Subtask 5.5: Configure health check failure alerts and logging

- [ ] Task 6: Add Unit and Integration Tests (Testing)
  - [ ] Subtask 6.1: Create unit tests for LocalFileStorageService with mocked file system
  - [ ] Subtask 6.2: Create unit tests for S3FileStorageService with mocked S3 client
  - [ ] Subtask 6.3: Create unit tests for IAiProcessingService with mocked Bedrock client
  - [ ] Subtask 6.4: Create integration tests for local file operations with temp directory
  - [ ] Subtask 6.5: Create integration tests for S3 operations with test bucket
  - [ ] Subtask 6.6: Create integration tests for Bedrock API calls with test prompts
  - [ ] Subtask 6.7: Test provider switching logic with different configuration values
  - [ ] Subtask 6.8: Add health check integration tests

## Dev Notes

- **Clean Architecture**: AWS services in `Medley.Infrastructure`, interfaces in `Medley.Application`, configuration in `Medley.Web`
- **Interface Abstractions**: Both `IFileStorageService` and `IAiProcessingService` allow easy swapping of implementations:
  - File Storage: S3 ↔ Azure Blob Storage ↔ Local FileSystem
  - AI Processing: AWS Bedrock ↔ OpenAI ↔ Azure OpenAI ↔ OpenRouter
- **Development vs Production**: 
  - Development: Use LocalFileStorageService (Provider = "Local") for fast local testing without AWS costs
  - Production: Use S3FileStorageService (Provider = "S3") for scalable cloud storage
  - Switch providers by changing FileStorage:Provider in appsettings.{Environment}.json
- **Security Best Practices**: 
  - Never commit AWS credentials to source control
  - Use environment variables or AWS IAM roles for production
  - Implement least-privilege IAM policies for S3 and Bedrock access
- **Cost Optimization**: Configure S3 lifecycle policies to transition old files to cheaper storage tiers
- **Epic 2 Readiness**: This story prepares infrastructure for AI fragment extraction in Epic 2

### Project Structure Notes

- **Configuration Classes** (strongly-typed):
  - `src/Medley.Application/Configuration/FileStorageSettings.cs` - File storage provider configuration
  - `src/Medley.Application/Configuration/AwsSettings.cs` - Root AWS configuration
  - `src/Medley.Application/Configuration/S3Settings.cs` - S3-specific settings
  - `src/Medley.Application/Configuration/BedrockSettings.cs` - Bedrock-specific settings
  - `src/Medley.Application/Enums/FileStorageProvider.cs` - Enum (Local, S3)
- **Interfaces**: 
  - `src/Medley.Application/Interfaces/IFileStorageService.cs`
  - `src/Medley.Application/Interfaces/IAiProcessingService.cs`
- **Implementations**:
  - `src/Medley.Infrastructure/Services/LocalFileStorageService.cs` - Local file system implementation
  - `src/Medley.Infrastructure/Services/S3FileStorageService.cs` - AWS S3 implementation
  - `src/Medley.Infrastructure/Services/BedrockAiService.cs` - AWS Bedrock implementation
- **Configuration**: 
  - `src/Medley.Web/appsettings.json` - Provider selection and settings (bound to strongly-typed classes)
  - `src/Medley.Web/appsettings.Development.json` - Development overrides (Provider = "Local")
  - `src/Medley.Web/appsettings.Production.json` - Production overrides (Provider = "S3")
  - `src/Medley.Infrastructure/DependencyInjection.cs` - Conditional service registration based on provider
- **Health Checks**: `src/Medley.Infrastructure/HealthChecks/` for AWS service health monitoring

**Key Implementation Patterns:**
- **Strongly-Typed Configuration**: Use IOptions<T> pattern for all settings (FileStorageSettings, AwsSettings, S3Settings, BedrockSettings)
- **Provider Pattern**: Register IFileStorageService implementation based on FileStorageSettings.Provider value
- **Configuration Binding**: Bind appsettings.json sections to configuration classes in DependencyInjection.cs
- **Constructor Injection**: Inject IOptions<FileStorageSettings>, IOptions<S3Settings>, IOptions<BedrockSettings> into service implementations
- **Conditional Registration**: Use switch statement on Provider enum to register correct IFileStorageService implementation
- Use `IAmazonS3` client from AWS SDK with proper disposal (S3 implementation only)
- Use `AmazonBedrockRuntimeClient` for Claude 4.5 invocations
- Implement retry logic with exponential backoff for transient failures
- Use structured logging for all operations
- Configure timeouts for S3 and Bedrock operations

**Provider Registration Example:**
```csharp
// In DependencyInjection.cs
var storageProvider = configuration.GetValue<FileStorageProvider>("FileStorage:Provider");
switch (storageProvider)
{
    case FileStorageProvider.Local:
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        break;
    case FileStorageProvider.S3:
        services.AddScoped<IFileStorageService, S3FileStorageService>();
        break;
}
```

**Example Configuration Structure:**
```json
{
  "FileStorage": {
    "Provider": "Local",  // "Local" for development, "S3" for production
    "LocalPath": "C:\\Medley\\Storage",  // Used when Provider = "Local"
    "FolderStructure": {
      "Documents": "documents/",
      "Uploads": "uploads/",
      "Temp": "temp/"
    }
  },
  "AWS": {
    "Region": "us-east-1",
    "AccessKeyId": "",
    "SecretAccessKey": "",
    "S3": {
      "BucketName": "medley-dev-storage",  // Used when Provider = "S3"
      "FolderStructure": {
        "Documents": "documents/",
        "Uploads": "uploads/",
        "Temp": "temp/"
      }
    },
    "Bedrock": {
      "ModelId": "anthropic.claude-3-5-sonnet-20241022-v2:0",
      "MaxTokens": 4096,
      "Temperature": 0.7
    }
  }
}
```

**Lessons from Story 1.5**: Leverage existing patterns from Hangfire integration - maintain Clean Architecture, ensure proper DI registration, and include comprehensive testing.

### References

- [Source: docs/epics.md#Story 1.6: AWS Integration Setup]
- [Source: docs/tech-spec-epic-1.md#AWS Integration (Story 1.6)]
- [Source: docs/solution-architecture.md#Technology Stack - AWS S3 and Bedrock]
- [Source: docs/solution-architecture.md#Key Interface Abstractions]
- [Source: docs/PRD.md#Functional Requirements - FR007-FR011: AI Fragment Extraction]

## Dev Agent Record

### Context Reference

- docs/stories/story-context-1.6.xml

### Agent Model Used

Claude 3.5 Sonnet (Kiro)

### Debug Log References

### Completion Notes List

### File List

## Change Log

- 2025-10-20: Story created from Epic 1 breakdown with AWS SDK, S3, and Bedrock integration
- 2025-10-20: Updated to include strongly-typed configuration classes using IOptions pattern per user request
- 2025-10-20: Added LocalFileStorageService implementation alongside S3FileStorageService with provider-switching mechanism per user request
