# Design Document: Vue 3 TypeScript Conversion

## Overview

This design outlines the conversion of the Medley application to use TypeScript across both frontend and backend, with automatic type generation to keep them synchronized. The frontend Vue 3 application will be converted from JavaScript to TypeScript with strongly-typed DTOs, typed SignalR hub connections, and a generic typed API client. The backend C# application will be enhanced with strongly-typed SignalR hubs and NSwag-based automatic TypeScript type generation from C# DTOs.

The design follows Vue 3 Composition API best practices using `<script setup lang="ts">` syntax and eliminates legacy mixins in favor of typed composables. On the backend, SignalR hubs will use Hub<TClient> for compile-time type safety, and NSwag will automatically generate TypeScript types and API clients from OpenAPI specifications.

The conversion will be incremental, allowing the application to remain functional throughout the migration process. TypeScript's strict mode will be enabled to maximize type safety benefits.

Commit after each major task is complete.

## Architecture

### High-Level Structure

**Frontend Structure:**
```
src/Medley.Web/Vue/
├── types/                    # TypeScript type definitions
│   ├── generated/           # Auto-generated from C# via NSwag
│   │   ├── api-client.ts   # Generated API client
│   │   └── dtos.ts         # Generated DTO interfaces
│   ├── signalr/            # SignalR hub type definitions
│   │   ├── article-hub.ts
│   │   ├── admin-hub.ts
│   │   └── index.ts
│   └── index.ts            # Central type exports
├── utils/
│   ├── api.ts              # Typed API client wrapper
│   ├── signalr.ts          # Typed SignalR utilities
│   ├── helpers.ts
│   ├── htmlDiff.ts
│   └── url.ts
├── composables/            # Typed composables (converted from mixins)
│   ├── useArticleTree.ts
│   ├── useArticleVersions.ts
│   ├── useArticleView.ts
│   ├── useMyWork.ts
│   ├── useSidebarState.ts
│   ├── useArticleSignalR.ts  # Converted from mixin
│   ├── useArticleFilter.ts   # Converted from mixin
│   └── useArticleModal.ts    # Converted from mixin
├── components/             # TypeScript Vue components
│   ├── ArticleList.vue
│   ├── ArticleTree.vue
│   ├── ChatPanel.vue
│   └── ...
├── pages/                  # TypeScript Vue pages
│   ├── Articles.vue
│   ├── Dashboard.vue
│   └── ...
├── router/
│   └── index.ts            # Typed router configuration
├── App.vue
└── main.ts
```

**Backend Structure:**
```
src/Medley.Application/
├── Hubs/
│   ├── ArticleHub.cs       # Strongly-typed hub
│   ├── AdminHub.cs         # Strongly-typed hub
│   └── Clients/            # Client interface definitions
│       ├── IArticleClient.cs
│       └── IAdminClient.cs
├── Models/
│   └── DTOs/               # Data Transfer Objects
│       ├── ArticleDto.cs
│       ├── FragmentDto.cs
│       └── ...
└── Services/
    └── ...

src/Medley.Web/
├── Controllers/
│   └── Api/                # Strongly-typed API controllers
│       ├── ArticlesApiController.cs
│       ├── FragmentsApiController.cs
│       └── ...
├── nswag.json              # NSwag configuration
└── ...
```

### TypeScript Configuration

The project will use a strict TypeScript configuration with the following key settings:

- **Strict mode enabled**: Catches more potential errors at compile time
- **No implicit any**: Forces explicit typing
- **Strict null checks**: Prevents null/undefined errors
- **ES module interop**: Allows importing CommonJS modules
- **Path aliases**: Maintains existing `@/` alias for imports
- **Vue-specific settings**: Proper handling of `.vue` files

### Build Pipeline

The Vite build pipeline will be updated to:
1. Type-check TypeScript files before compilation
2. Generate source maps for debugging
3. Support mixed .js/.ts files during migration
4. Maintain existing build output structure

The C# build pipeline will be updated to:
1. Generate OpenAPI specification from API controllers
2. Run NSwag to generate TypeScript types and client
3. Place generated files in Vue/types/generated/
4. Ensure generated types are available before Vue build

## Backend Type Safety

### Strongly-Typed SignalR Hubs

The C# SignalR hubs will be converted to use strongly-typed client interfaces, providing compile-time safety for server-to-client method calls.

**Client Interface Definition** (`Hubs/Clients/IArticleClient.cs`):
```csharp
namespace Medley.Application.Hubs.Clients;

/// <summary>
/// Strongly-typed interface for ArticleHub server-to-client methods
/// </summary>
public interface IArticleClient
{
    Task ArticleCreated(ArticleCreatedPayload payload);
    Task ArticleUpdated(ArticleUpdatedPayload payload);
    Task ArticleDeleted(ArticleDeletedPayload payload);
    Task ArticleMoved(ArticleMovedPayload payload);
    Task ArticleAssignmentChanged(ArticleAssignmentChangedPayload payload);
    Task VersionCreated(VersionCreatedPayload payload);
    Task PlanGenerated(PlanGeneratedPayload payload);
    Task ArticleVersionCreated(ArticleVersionCreatedPayload payload);
    Task ChatTurnStarted(ChatTurnStartedPayload payload);
    Task ChatTurnComplete(ChatTurnCompletePayload payload);
    Task ChatMessageStreaming(ChatStreamUpdate payload);
    Task ReceiveMessage(object payload);
}

/// <summary>
/// Payload for ArticleCreated event
/// </summary>
public record ArticleCreatedPayload(
    string ArticleId,
    string Title,
    string? ParentArticleId,
    int ArticleTypeId,
    DateTimeOffset Timestamp
);

/// <summary>
/// Payload for ArticleUpdated event
/// </summary>
public record ArticleUpdatedPayload(
    string ArticleId,
    string Title,
    int ArticleTypeId,
    DateTimeOffset Timestamp
);

// Additional payload records...
```

**Strongly-Typed Hub** (`Hubs/ArticleHub.cs`):
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Medley.Application.Hubs.Clients;

namespace Medley.Application.Hubs;

/// <summary>
/// Strongly-typed SignalR hub for real-time article updates and chat
/// </summary>
[Authorize]
public class ArticleHub : Hub<IArticleClient>
{
    /// <summary>
    /// Join a specific article's chat room
    /// </summary>
    public async Task JoinArticle(string articleId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Article_{articleId}");
    }

    /// <summary>
    /// Leave a specific article's chat room
    /// </summary>
    public async Task LeaveArticle(string articleId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Article_{articleId}");
    }

    /// <summary>
    /// Send a chat message to an article's room
    /// </summary>
    public async Task SendMessage(string articleId, string message)
    {
        var userName = Context.User?.Identity?.Name ?? "Anonymous";
        
        await Clients.Group($"Article_{articleId}").ReceiveMessage(new
        {
            ArticleId = articleId,
            UserName = userName,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}
```

**Strongly-Typed Hub Context Usage** (in services/background jobs):
```csharp
public class ArticleService
{
    private readonly IHubContext<ArticleHub, IArticleClient> _articleHubContext;

    public ArticleService(IHubContext<ArticleHub, IArticleClient> articleHubContext)
    {
        _articleHubContext = articleHubContext;
    }

    public async Task NotifyArticleCreated(string articleId, string title, string? parentArticleId)
    {
        // Strongly-typed method call - compile-time checked!
        await _articleHubContext.Clients.All.ArticleCreated(new ArticleCreatedPayload(
            articleId,
            title,
            parentArticleId,
            1,
            DateTimeOffset.UtcNow
        ));
    }
}
```

**AdminHub Client Interface** (`Hubs/Clients/IAdminClient.cs`):
```csharp
namespace Medley.Application.Hubs.Clients;

/// <summary>
/// Strongly-typed interface for AdminHub server-to-client methods
/// </summary>
public interface IAdminClient
{
    Task AdminNotification(AdminNotificationPayload payload);
}

/// <summary>
/// Payload for admin notifications
/// </summary>
public record AdminNotificationPayload(
    string Message,
    string Severity,
    DateTimeOffset Timestamp
);
```

### NSwag Type Generation

NSwag will automatically generate TypeScript types and API client from the C# backend, ensuring frontend and backend types stay synchronized.

**NSwag Configuration** (`nswag.json`):
```json
{
  "runtime": "Net80",
  "defaultVariables": null,
  "documentGenerator": {
    "aspNetCoreToOpenApi": {
      "project": "Medley.Web.csproj",
      "msBuildProjectExtensionsPath": null,
      "configuration": null,
      "runtime": null,
      "targetFramework": null,
      "noBuild": false,
      "verbose": false,
      "workingDirectory": null,
      "requireParametersWithoutDefault": true,
      "apiGroupNames": null,
      "defaultPropertyNameHandling": "Default",
      "defaultReferenceTypeNullHandling": "Null",
      "defaultDictionaryValueReferenceTypeNullHandling": "NotNull",
      "defaultResponseReferenceTypeNullHandling": "NotNull",
      "defaultEnumHandling": "Integer",
      "flattenInheritanceHierarchy": false,
      "generateKnownTypes": true,
      "generateEnumMappingDescription": false,
      "generateXmlObjects": false,
      "generateAbstractProperties": false,
      "generateAbstractSchemas": true,
      "ignoreObsoleteProperties": false,
      "allowReferencesWithProperties": false,
      "excludedTypeNames": [],
      "serviceHost": null,
      "serviceBasePath": null,
      "serviceSchemes": [],
      "infoTitle": "Medley API",
      "infoDescription": null,
      "infoVersion": "1.0.0",
      "documentTemplate": null,
      "documentProcessorTypes": [],
      "operationProcessorTypes": [],
      "typeNameGeneratorType": null,
      "schemaNameGeneratorType": null,
      "contractResolverType": null,
      "serializerSettingsType": null,
      "useDocumentProvider": true,
      "documentName": "v1",
      "aspNetCoreEnvironment": null,
      "createWebHostBuilderMethod": null,
      "startupType": null,
      "allowNullableBodyParameters": true,
      "output": "wwwroot/swagger/v1/swagger.json",
      "outputType": "OpenApi3",
      "assemblyPaths": [],
      "assemblyConfig": null,
      "referencePaths": [],
      "useNuGetCache": false
    }
  },
  "codeGenerators": {
    "openApiToTypeScriptClient": {
      "className": "MedleyApiClient",
      "moduleName": "",
      "namespace": "",
      "typeScriptVersion": 5.0,
      "template": "Fetch",
      "promiseType": "Promise",
      "httpClass": "HttpClient",
      "withCredentials": false,
      "useSingletonProvider": false,
      "injectionTokenType": "OpaqueToken",
      "rxJsVersion": 7.0,
      "dateTimeType": "Date",
      "nullValue": "Null",
      "generateClientClasses": true,
      "generateClientInterfaces": false,
      "generateOptionalParameters": true,
      "exportTypes": true,
      "wrapDtoExceptions": true,
      "exceptionClass": "ApiException",
      "clientBaseClass": null,
      "wrapResponses": false,
      "wrapResponseMethods": [],
      "generateResponseClasses": true,
      "responseClass": "SwaggerResponse",
      "protectedMethods": [],
      "configurationClass": null,
      "useTransformOptionsMethod": false,
      "useTransformResultMethod": false,
      "generateDtoTypes": true,
      "operationGenerationMode": "MultipleClientsFromOperationId",
      "markOptionalProperties": true,
      "generateCloneMethod": false,
      "typeStyle": "Interface",
      "enumStyle": "Enum",
      "useLeafType": false,
      "classTypes": [],
      "extendedClasses": [],
      "extensionCode": null,
      "generateDefaultValues": true,
      "excludedTypeNames": [],
      "excludedParameterNames": [],
      "handleReferences": false,
      "generateConstructorInterface": true,
      "convertConstructorInterfaceData": false,
      "importRequiredTypes": true,
      "useGetBaseUrlMethod": false,
      "baseUrlTokenName": "API_BASE_URL",
      "queryNullValue": "",
      "useAbortSignal": false,
      "inlineNamedDictionaries": false,
      "inlineNamedAny": false,
      "includeHttpContext": false,
      "templateDirectory": null,
      "typeNameGeneratorType": null,
      "propertyNameGeneratorType": null,
      "enumNameGeneratorType": null,
      "serviceHost": null,
      "serviceSchemes": null,
      "output": "Vue/types/generated/api-client.ts",
      "newLineBehavior": "Auto"
    }
  }
}
```

**Generated TypeScript Output** (`Vue/types/generated/api-client.ts`):
```typescript
// Auto-generated by NSwag - DO NOT EDIT

export interface Article {
    id: string;
    title: string;
    content: string;
    parentArticleId: string | null;
    articleTypeId: number;
    assignedUser: UserSummary | null;
    children: Article[];
    currentConversation: ConversationSummary | null;
    createdAt: Date;
    updatedAt: Date;
}

export interface ArticleCreateRequest {
    title: string;
    parentArticleId: string | null;
    articleTypeId: number;
}

export interface ArticleUpdateRequest {
    title: string;
    content: string;
    articleTypeId: number;
}

// ... more generated types

export class MedleyApiClient {
    private http: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> };
    private baseUrl: string;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined = undefined;

    constructor(baseUrl?: string, http?: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> }) {
        this.http = http ? http : window as any;
        this.baseUrl = baseUrl !== undefined && baseUrl !== null ? baseUrl : "";
    }

    /**
     * Get article by ID
     * @param id Article ID
     * @return Article found
     */
    getArticle(id: string): Promise<Article> {
        let url_ = this.baseUrl + "/api/articles/{id}";
        if (id === undefined || id === null)
            throw new Error("The parameter 'id' must be defined.");
        url_ = url_.replace("{id}", encodeURIComponent("" + id));
        url_ = url_.replace(/[?&]$/, "");

        let options_: RequestInit = {
            method: "GET",
            headers: {
                "Accept": "application/json"
            }
        };

        return this.http.fetch(url_, options_).then((_response: Response) => {
            return this.processGetArticle(_response);
        });
    }

    // ... more generated methods
}
```

### API Controller Type Safety

API controllers will use strongly-typed DTOs with proper annotations for OpenAPI generation.

**Strongly-Typed Controller Example**:
```csharp
using Microsoft.AspNetCore.Mvc;
using Medley.Application.Models.DTOs;

namespace Medley.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ArticlesApiController : ControllerBase
{
    /// <summary>
    /// Get article by ID
    /// </summary>
    /// <param name="id">Article ID</param>
    /// <returns>Article details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ArticleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArticleDto>> GetArticle(Guid id)
    {
        var article = await _articleService.GetByIdAsync(id);
        
        if (article == null)
        {
            return NotFound();
        }
        
        return Ok(article);
    }

    /// <summary>
    /// Create a new article
    /// </summary>
    /// <param name="request">Article creation request</param>
    /// <returns>Created article</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ArticleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ArticleDto>> CreateArticle([FromBody] ArticleCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var article = await _articleService.CreateAsync(request);
        
        return CreatedAtAction(nameof(GetArticle), new { id = article.Id }, article);
    }

    /// <summary>
    /// Update an existing article
    /// </summary>
    /// <param name="id">Article ID</param>
    /// <param name="request">Article update request</param>
    /// <returns>Updated article</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ArticleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ArticleDto>> UpdateArticle(Guid id, [FromBody] ArticleUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var article = await _articleService.UpdateAsync(id, request);
        
        if (article == null)
        {
            return NotFound();
        }
        
        return Ok(article);
    }

    /// <summary>
    /// Delete an article
    /// </summary>
    /// <param name="id">Article ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteArticle(Guid id)
    {
        var success = await _articleService.DeleteAsync(id);
        
        if (!success)
        {
            return NotFound();
        }
        
        return NoContent();
    }
}
```

**DTO Definition with Validation**:
```csharp
using System.ComponentModel.DataAnnotations;

namespace Medley.Application.Models.DTOs;

/// <summary>
/// Request to create a new article
/// </summary>
public class ArticleCreateRequest
{
    /// <summary>
    /// Article title
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Parent article ID (null for root articles)
    /// </summary>
    public Guid? ParentArticleId { get; set; }

    /// <summary>
    /// Article type ID
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int ArticleTypeId { get; set; }
}

/// <summary>
/// Request to update an existing article
/// </summary>
public class ArticleUpdateRequest
{
    /// <summary>
    /// Article title
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Article content (markdown)
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Article type ID
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int ArticleTypeId { get; set; }
}

/// <summary>
/// Article data transfer object
/// </summary>
public class ArticleDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? ParentArticleId { get; set; }
    public int ArticleTypeId { get; set; }
    public UserSummaryDto? AssignedUser { get; set; }
    public List<ArticleDto> Children { get; set; } = new();
    public ConversationSummaryDto? CurrentConversation { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

## Components and Interfaces

### Type System Organization

#### DTO Type Definitions

DTOs will primarily be auto-generated by NSwag from C# backend models. The generated types will be placed in `types/generated/` directory. Custom type extensions and SignalR-specific types will be manually maintained in separate files.

**Generated DTOs** (`types/generated/api-client.ts` - auto-generated by NSwag):
All API request/response DTOs will be automatically generated from C# models, including:
- Article, ArticleCreateRequest, ArticleUpdateRequest
- Fragment, FragmentSearchResult
- User, UserSummary
- Conversation, ChatMessage, ToolCall
- ArticleVersion, ArticleVersionComparison
- Plan, PlanStatus
- Source, SourceType

**Manual Type Extensions** (`types/dtos/extensions.ts`):
Additional types not covered by NSwag generation:
```typescript
// Re-export generated types for convenience
export * from '../generated/api-client';

// Additional types for SignalR payloads (not part of REST API)
export interface ConversationSummary {
  id: string;
  isRunning: boolean;
}

// Enum for stream update types (used in SignalR, not REST API)
export enum StreamUpdateType {
  TextDelta = 'TextDelta',
  ToolCall = 'ToolCall',
  ToolResult = 'ToolResult',
  MessageComplete = 'MessageComplete',
  TurnComplete = 'TurnComplete'
}

// Chat stream update (SignalR-specific)
export interface ChatStreamUpdate {
  type: StreamUpdateType;
  conversationId: string;
  articleId: string | null;
  text: string | null;
  toolName: string | null;
  toolCallId: string | null;
  toolDisplay: string | null;
  toolResultIds: string[] | null;
  isError: boolean | null;
  messageId: string | null;
  timestamp: string;
}
```

#### SignalR Hub Type Definitions

SignalR hub connections will be strongly typed using TypeScript interfaces that define server-to-client method signatures.

**ArticleHub Types** (`types/signalr/article-hub.ts`):
```typescript
import type { HubConnection } from '@microsoft/signalr';
import type { ChatStreamUpdate } from '../dtos/chat';

// Server-to-client method payloads
export interface ArticleCreatedPayload {
  articleId: string;
  title: string;
  parentArticleId: string | null;
  articleTypeId: number;
  timestamp: string;
}

export interface ArticleUpdatedPayload {
  articleId: string;
  title: string;
  articleTypeId: number;
  timestamp: string;
}

export interface ArticleDeletedPayload {
  articleId: string;
  timestamp: string;
}

export interface ArticleMovedPayload {
  articleId: string;
  oldParentId: string | null;
  newParentId: string | null;
  timestamp: string;
}

export interface ArticleAssignmentChangedPayload {
  articleId: string;
  userId: string | null;
  userName: string | null;
  userInitials: string | null;
  userColor: string | null;
  timestamp: string;
}

export interface VersionCreatedPayload {
  articleId: string;
  versionId: string;
  versionNumber: number;
  createdAt: string;
}

export interface PlanGeneratedPayload {
  articleId: string;
  planId: string;
  timestamp: string;
}

export interface ArticleVersionCreatedPayload {
  articleId: string;
  versionId: string;
  versionNumber: number;
  timestamp: string;
}

export interface ChatTurnStartedPayload {
  conversationId: string;
  articleId: string;
}

export interface ChatTurnCompletePayload {
  conversationId: string;
  articleId: string;
}

// Server-to-client methods interface
export interface ArticleHubServerMethods {
  ArticleCreated: (payload: ArticleCreatedPayload) => void;
  ArticleUpdated: (payload: ArticleUpdatedPayload) => void;
  ArticleDeleted: (payload: ArticleDeletedPayload) => void;
  ArticleMoved: (payload: ArticleMovedPayload) => void;
  ArticleAssignmentChanged: (payload: ArticleAssignmentChangedPayload) => void;
  VersionCreated: (payload: VersionCreatedPayload) => void;
  PlanGenerated: (payload: PlanGeneratedPayload) => void;
  ArticleVersionCreated: (payload: ArticleVersionCreatedPayload) => void;
  ChatTurnStarted: (payload: ChatTurnStartedPayload) => void;
  ChatTurnComplete: (payload: ChatTurnCompletePayload) => void;
  ChatMessageStreaming: (payload: ChatStreamUpdate) => void;
  ReceiveMessage: (payload: any) => void;
}

// Typed hub connection
export type ArticleHubConnection = HubConnection & {
  on<K extends keyof ArticleHubServerMethods>(
    methodName: K,
    handler: ArticleHubServerMethods[K]
  ): void;
};
```

**AdminHub Types** (`types/signalr/admin-hub.ts`):
```typescript
import type { HubConnection } from '@microsoft/signalr';

// Server-to-client method payloads
export interface AdminNotificationPayload {
  message: string;
  severity: 'info' | 'warning' | 'error';
  timestamp: string;
}

// Server-to-client methods interface
export interface AdminHubServerMethods {
  AdminNotification: (payload: AdminNotificationPayload) => void;
}

// Typed hub connection
export type AdminHubConnection = HubConnection & {
  on<K extends keyof AdminHubServerMethods>(
    methodName: K,
    handler: AdminHubServerMethods[K]
  ): void;
};
```

### Typed API Client

The API client will wrap the NSwag-generated client to provide a consistent interface and additional error handling.

**API Client Wrapper** (`utils/api.ts`):
```typescript
import { MedleyApiClient, ApiException } from '@/types/generated/api-client';

// Re-export ApiException for convenience
export { ApiException };

// Create singleton instance
const generatedClient = new MedleyApiClient(window.location.origin);

// Wrapper class for additional functionality
class ApiClientWrapper {
  private client: MedleyApiClient;

  constructor(client: MedleyApiClient) {
    this.client = client;
  }

  // Expose generated client methods
  get articles() {
    return {
      getById: (id: string) => this.client.getArticle(id),
      create: (request: ArticleCreateRequest) => this.client.createArticle(request),
      update: (id: string, request: ArticleUpdateRequest) => this.client.updateArticle(id, request),
      delete: (id: string) => this.client.deleteArticle(id),
      getAll: () => this.client.getAllArticles()
    };
  }

  get fragments() {
    return {
      search: (query: string) => this.client.searchFragments(query),
      getById: (id: string) => this.client.getFragment(id)
    };
  }

  get sources() {
    return {
      getAll: () => this.client.getAllSources(),
      getById: (id: string) => this.client.getSource(id)
    };
  }

  // Add more resource groups as needed
}

export const api = new ApiClientWrapper(generatedClient);

// For direct access to generated client if needed
export const rawClient = generatedClient;
```

**Usage Example**:
```typescript
import { api, ApiException } from '@/utils/api';

// Type-safe API calls with auto-generated types
try {
  const article = await api.articles.getById('123');
  console.log(article.title); // TypeScript knows all properties
  
  const newArticle = await api.articles.create({
    title: 'New Article',
    parentArticleId: null,
    articleTypeId: 1
  });
} catch (error) {
  if (error instanceof ApiException) {
    console.error(`API error ${error.status}: ${error.message}`);
  } else {
    console.error('Unexpected error:', error);
  }
}
```

### Typed SignalR Utilities

**SignalR Connection Factory** (`utils/signalr.ts`):
```typescript
import * as signalR from '@microsoft/signalr';
import type { ArticleHubConnection } from '@/types/signalr/article-hub';
import type { AdminHubConnection } from '@/types/signalr/admin-hub';

export function createArticleHubConnection(): ArticleHubConnection {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/articleHub')
    .withAutomaticReconnect()
    .build() as ArticleHubConnection;

  connection.onreconnecting((error) => {
    console.log('SignalR reconnecting...', error);
  });

  connection.onreconnected((connectionId) => {
    console.log('SignalR reconnected:', connectionId);
  });

  connection.onclose((error) => {
    console.log('SignalR connection closed:', error);
  });

  return connection;
}

export function createAdminHubConnection(): AdminHubConnection {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/adminHub')
    .withAutomaticReconnect()
    .build() as AdminHubConnection;

  connection.onreconnecting((error) => {
    console.log('AdminHub reconnecting...', error);
  });

  connection.onreconnected((connectionId) => {
    console.log('AdminHub reconnected:', connectionId);
  });

  connection.onclose((error) => {
    console.log('AdminHub connection closed:', error);
  });

  return connection;
}
```

### Component Conversion Pattern

All Vue components will be converted to use `<script setup lang="ts">` syntax with proper typing.

**Component Conversion Example**:
```vue
<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { api } from '@/utils/api';
import type { Article } from '@/types/dtos/article';

// Props with TypeScript interface
interface Props {
  articleId: string;
  readonly?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  readonly: false
});

// Emits with typed payloads
interface Emits {
  (e: 'update', article: Article): void;
  (e: 'delete', articleId: string): void;
}

const emit = defineEmits<Emits>();

// Typed reactive state
const article = ref<Article | null>(null);
const loading = ref<boolean>(false);
const error = ref<string | null>(null);

// Computed with inferred type
const isEditable = computed(() => !props.readonly && article.value !== null);

// Typed async function
async function loadArticle(): Promise<void> {
  loading.value = true;
  error.value = null;
  
  try {
    article.value = await api.get<Article>(`/api/articles/${props.articleId}`);
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unknown error';
  } finally {
    loading.value = false;
  }
}

// Lifecycle hooks
onMounted(() => {
  loadArticle();
});
</script>
```

### Composable Conversion Pattern

Mixins will be converted to composables with proper TypeScript typing and return type definitions.

**Composable Conversion Example** (`composables/useArticleSignalR.ts`):
```typescript
import { ref, onMounted, onUnmounted } from 'vue';
import { createArticleHubConnection } from '@/utils/signalr';
import type { ArticleHubConnection } from '@/types/signalr/article-hub';
import type { Article } from '@/types/dtos/article';

interface SignalRQueueUpdate {
  type: 'ArticleCreated' | 'ArticleUpdated' | 'ArticleDeleted' | 'ArticleMoved' | 'ArticleAssignmentChanged';
  data: any;
}

interface UseArticleSignalROptions {
  onArticleCreated?: (article: Article) => void;
  onArticleUpdated?: (articleId: string, updates: Partial<Article>) => void;
  onArticleDeleted?: (articleId: string) => void;
  onArticleMoved?: (articleId: string, oldParentId: string | null, newParentId: string | null) => void;
}

export function useArticleSignalR(options: UseArticleSignalROptions = {}) {
  const connection = ref<ArticleHubConnection | null>(null);
  const updateQueue = ref<SignalRQueueUpdate[]>([]);
  const processing = ref<boolean>(false);
  const connected = ref<boolean>(false);

  function processQueue(): void {
    if (processing.value || updateQueue.value.length === 0) {
      return;
    }

    processing.value = true;

    const queue = [...updateQueue.value];
    updateQueue.value = [];

    queue.forEach(update => {
      switch (update.type) {
        case 'ArticleCreated':
          options.onArticleCreated?.(update.data);
          break;
        case 'ArticleUpdated':
          options.onArticleUpdated?.(update.data.articleId, update.data.updates);
          break;
        case 'ArticleDeleted':
          options.onArticleDeleted?.(update.data.articleId);
          break;
        case 'ArticleMoved':
          options.onArticleMoved?.(
            update.data.articleId,
            update.data.oldParentId,
            update.data.newParentId
          );
          break;
      }
    });

    processing.value = false;
  }

  async function connect(): Promise<void> {
    connection.value = createArticleHubConnection();

    // Register event handlers with proper typing
    connection.value.on('ArticleCreated', (payload) => {
      updateQueue.value.push({
        type: 'ArticleCreated',
        data: {
          id: payload.articleId,
          title: payload.title,
          parentArticleId: payload.parentArticleId,
          articleTypeId: payload.articleTypeId,
          children: []
        }
      });
      processQueue();
    });

    connection.value.on('ArticleUpdated', (payload) => {
      updateQueue.value.push({
        type: 'ArticleUpdated',
        data: {
          articleId: payload.articleId,
          updates: {
            title: payload.title,
            articleTypeId: payload.articleTypeId
          }
        }
      });
      processQueue();
    });

    // Additional event handlers...

    await connection.value.start();
    connected.value = true;
    console.log('Connected to ArticleHub');
  }

  async function disconnect(): Promise<void> {
    if (connection.value) {
      await connection.value.stop();
      connected.value = false;
      console.log('Disconnected from ArticleHub');
    }
  }

  async function joinArticle(articleId: string): Promise<void> {
    if (connection.value && connected.value) {
      await connection.value.invoke('JoinArticle', articleId);
    }
  }

  async function leaveArticle(articleId: string): Promise<void> {
    if (connection.value && connected.value) {
      await connection.value.invoke('LeaveArticle', articleId);
    }
  }

  onMounted(() => {
    connect();
  });

  onUnmounted(() => {
    disconnect();
  });

  return {
    connection,
    connected,
    joinArticle,
    leaveArticle,
    disconnect
  };
}
```

## Data Models

### Type Organization

Types are organized hierarchically:

1. **Base DTOs**: Core data structures (`types/dtos/`)
2. **SignalR Types**: Hub connection and event types (`types/signalr/`)
3. **Component Types**: Component-specific interfaces (defined inline in components)
4. **Utility Types**: Helper types for common patterns (`types/utils.ts`)

### Utility Types

**Common Utility Types** (`types/utils.ts`):
```typescript
// Nullable type helper
export type Nullable<T> = T | null;

// API response wrapper
export type ApiResult<T> = {
  data: T;
  success: boolean;
  error?: string;
};

// Pagination types
export interface PaginatedRequest {
  page: number;
  pageSize: number;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Loading state helper
export interface LoadingState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*


### Property Reflection

Reviewing the testable properties identified in prework:

**API Client Properties (4.1-4.7)**:
- Properties 4.1-4.4 test that each HTTP method (GET, POST, PUT, DELETE) works correctly
- These can be combined into a single comprehensive property about HTTP method correctness
- Property 4.6 (error throwing) is distinct and should remain separate
- Property 4.7 (response validation) is distinct and should remain separate
- Property 4.5 (204 handling) is an edge case that will be covered by generators

**Validation Properties (11.1-11.5)**:
- Property 11.1 (API response validation) and 11.2 (SignalR message validation) test the same validation mechanism on different inputs
- These can be combined into a single property about validation correctness
- Property 11.4 (graceful error handling) is distinct
- Property 11.5 (type guard correctness) is distinct

**Final Property Set**:
1. HTTP methods return correctly typed responses for valid requests
2. API client throws typed errors for failed requests
3. Validation functions correctly identify valid and invalid data structures
4. System handles validation failures gracefully without crashing
5. Type guard functions correctly distinguish valid from invalid data

### Correctness Properties

Property 1: HTTP Method Type Safety
*For any* valid HTTP request (GET, POST, PUT, DELETE) with appropriate request data, the API client should return a response that matches the expected TypeScript type or null for 204 responses
**Validates: Requirements 4.1, 4.2, 4.3, 4.4**

Property 2: API Error Typing
*For any* failed HTTP request (4xx or 5xx status), the API client should throw an ApiError with correctly typed status, statusText, and message properties
**Validates: Requirements 4.6**

Property 3: Runtime Validation Correctness
*For any* data structure and corresponding type guard function, the type guard should return true for valid data matching the type definition and false for invalid data that doesn't match
**Validates: Requirements 11.1, 11.2, 11.5**

Property 4: Graceful Validation Failure Handling
*For any* validation failure in API responses or SignalR messages, the system should continue operating without throwing unhandled exceptions or crashing the application
**Validates: Requirements 11.4**

Property 5: C# SignalR Hub Type Safety
*For any* strongly-typed SignalR hub method call from C# code, the compiler should enforce correct method names and parameter types at compile time
**Validates: Requirements 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 15.7**

Property 6: Generated TypeScript Type Accuracy
*For any* C# DTO class with properties, the NSwag-generated TypeScript interface should contain equivalent properties with correctly mapped types
**Validates: Requirements 16.3, 16.4**

## Error Handling

### API Client Error Handling

The typed API client will handle errors consistently:

1. **Network Errors**: Fetch failures will propagate as-is
2. **HTTP Errors**: Non-2xx responses throw `ApiError` with status information
3. **JSON Parsing Errors**: Invalid JSON responses throw parsing errors
4. **Type Validation Errors**: Optional runtime validation can catch type mismatches

**Error Handling Pattern**:
```typescript
try {
  const article = await api.get<Article>(`/api/articles/${id}`);
  // Use article
} catch (error) {
  if (error instanceof ApiError) {
    // Handle API-specific errors
    console.error(`API error ${error.status}: ${error.message}`);
  } else {
    // Handle other errors (network, parsing, etc.)
    console.error('Unexpected error:', error);
  }
}
```

### SignalR Error Handling

SignalR connections will handle errors through:

1. **Connection Errors**: Logged and handled by automatic reconnection
2. **Message Handling Errors**: Caught and logged without crashing
3. **Validation Errors**: Invalid message payloads logged with type information

**SignalR Error Pattern**:
```typescript
connection.on('ArticleCreated', (payload) => {
  try {
    // Validate payload structure
    if (!isArticleCreatedPayload(payload)) {
      console.error('Invalid ArticleCreated payload:', payload);
      return;
    }
    
    // Process valid payload
    handleArticleCreated(payload);
  } catch (error) {
    console.error('Error handling ArticleCreated:', error);
  }
});
```

### Component Error Handling

Components will handle errors through:

1. **API Call Errors**: Stored in reactive error state and displayed to users
2. **Validation Errors**: Caught and displayed as form validation messages
3. **Unexpected Errors**: Caught by Vue error handlers and logged

**Component Error Pattern**:
```typescript
const error = ref<string | null>(null);

async function saveArticle() {
  error.value = null;
  
  try {
    await api.put<ArticleUpdateRequest, Article>(
      `/api/articles/${props.articleId}`,
      { title: title.value, content: content.value, articleTypeId: 1 }
    );
  } catch (err) {
    if (err instanceof ApiError) {
      error.value = `Failed to save: ${err.message}`;
    } else {
      error.value = 'An unexpected error occurred';
    }
  }
}
```

### Type Guard Implementation

Type guards will be implemented for critical DTOs to enable runtime validation:

```typescript
// Type guard for Article
export function isArticle(value: unknown): value is Article {
  if (typeof value !== 'object' || value === null) return false;
  
  const obj = value as Record<string, unknown>;
  
  return (
    typeof obj.id === 'string' &&
    typeof obj.title === 'string' &&
    typeof obj.content === 'string' &&
    (obj.parentArticleId === null || typeof obj.parentArticleId === 'string') &&
    typeof obj.articleTypeId === 'number' &&
    (obj.assignedUser === null || isUserSummary(obj.assignedUser)) &&
    Array.isArray(obj.children) &&
    typeof obj.createdAt === 'string' &&
    typeof obj.updatedAt === 'string'
  );
}

// Type guard for UserSummary
export function isUserSummary(value: unknown): value is UserSummary {
  if (typeof value !== 'object' || value === null) return false;
  
  const obj = value as Record<string, unknown>;
  
  return (
    typeof obj.id === 'string' &&
    typeof obj.fullName === 'string' &&
    typeof obj.initials === 'string' &&
    typeof obj.color === 'string'
  );
}
```

## Testing Strategy

### Dual Testing Approach

The TypeScript conversion will be validated through both unit tests and property-based tests:

**Unit Tests**:
- Specific examples of API client usage with mocked responses
- Edge cases like 204 No Content responses
- Error conditions (network failures, 4xx/5xx responses)
- Type guard validation with specific valid/invalid examples
- Component prop validation
- SignalR event handler behavior

**Property-Based Tests**:
- API client methods with randomly generated valid request/response data
- Type guard functions with randomly generated valid and invalid data structures
- Error handling with randomly generated error scenarios
- Validation functions with randomly generated payloads

### Property Test Configuration

All property-based tests will:
- Use Vitest with fast-check library for property-based testing
- Run minimum 100 iterations per test
- Tag each test with the design property it validates
- Use TypeScript for test files (.test.ts)

**Property Test Example**:
```typescript
import { describe, it, expect } from 'vitest';
import fc from 'fast-check';
import { api, ApiError } from '@/utils/api';
import type { Article } from '@/types/dtos/article';

describe('API Client', () => {
  /**
   * Feature: vue-typescript-conversion, Property 1: HTTP Method Type Safety
   * For any valid HTTP request, the API client should return correctly typed responses
   */
  it('should return typed responses for successful GET requests', async () => {
    await fc.assert(
      fc.asyncProperty(
        fc.record({
          id: fc.uuid(),
          title: fc.string(),
          content: fc.string(),
          parentArticleId: fc.option(fc.uuid(), { nil: null }),
          articleTypeId: fc.integer({ min: 1, max: 10 }),
          assignedUser: fc.constant(null),
          children: fc.constant([]),
          currentConversation: fc.constant(null),
          createdAt: fc.date().map(d => d.toISOString()),
          updatedAt: fc.date().map(d => d.toISOString())
        }),
        async (mockArticle) => {
          // Mock fetch to return the generated article
          global.fetch = vi.fn().mockResolvedValue({
            ok: true,
            status: 200,
            json: async () => mockArticle
          });

          const result = await api.get<Article>(`/api/articles/${mockArticle.id}`);
          
          expect(result).toEqual(mockArticle);
          expect(result?.id).toBe(mockArticle.id);
          expect(result?.title).toBe(mockArticle.title);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: vue-typescript-conversion, Property 2: API Error Typing
   * For any failed HTTP request, the API client should throw typed errors
   */
  it('should throw ApiError for failed requests', async () => {
    await fc.assert(
      fc.asyncProperty(
        fc.integer({ min: 400, max: 599 }),
        fc.string({ minLength: 1 }),
        async (status, statusText) => {
          global.fetch = vi.fn().mockResolvedValue({
            ok: false,
            status,
            statusText
          });

          await expect(api.get<Article>('/api/articles/123'))
            .rejects
            .toThrow(ApiError);

          try {
            await api.get<Article>('/api/articles/123');
          } catch (error) {
            expect(error).toBeInstanceOf(ApiError);
            expect((error as ApiError).status).toBe(status);
            expect((error as ApiError).statusText).toBe(statusText);
          }
        }
      ),
      { numRuns: 100 }
    );
  });
});
```

### Unit Test Examples

**Type Guard Unit Tests**:
```typescript
import { describe, it, expect } from 'vitest';
import { isArticle, isUserSummary } from '@/types/dtos/article';

describe('Type Guards', () => {
  it('should validate correct Article structure', () => {
    const validArticle = {
      id: '123',
      title: 'Test',
      content: 'Content',
      parentArticleId: null,
      articleTypeId: 1,
      assignedUser: null,
      children: [],
      currentConversation: null,
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z'
    };

    expect(isArticle(validArticle)).toBe(true);
  });

  it('should reject invalid Article structure', () => {
    const invalidArticle = {
      id: 123, // Should be string
      title: 'Test'
    };

    expect(isArticle(invalidArticle)).toBe(false);
  });

  it('should handle 204 No Content response', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 204
    });

    const result = await api.delete('/api/articles/123');
    expect(result).toBe(true);
  });
});
```

### Migration Testing Strategy

During the incremental migration:

1. **Parallel Testing**: Run tests on both JS and TS versions of converted files
2. **Behavior Preservation**: Ensure converted components behave identically to originals
3. **Type Coverage**: Track percentage of codebase with TypeScript types
4. **Build Validation**: Ensure application builds and runs at each migration step
5. **Integration Testing**: Test interactions between converted and unconverted code

### Testing Tools

- **Vitest**: Test runner with TypeScript support
- **fast-check**: Property-based testing library
- **@vue/test-utils**: Vue component testing utilities
- **MSW (Mock Service Worker)**: API mocking for integration tests
- **TypeScript Compiler**: Type checking as part of test suite

## Implementation Notes

### Migration Phases

The conversion will proceed in phases to maintain application stability:

**Phase 1: Infrastructure Setup**
- Add TypeScript dependencies
- Create tsconfig.json
- Update Vite configuration
- Set up type definition structure

**Phase 2: Core Type Definitions**
- Create all DTO interfaces
- Create SignalR hub type definitions
- Create utility types

**Phase 3: Typed Utilities**
- Convert and type API client
- Convert and type SignalR utilities
- Convert and type helper functions

**Phase 4: Composables**
- Convert mixins to typed composables
- Update existing composables with types
- Add type guards where needed

**Phase 5: Components**
- Convert core components (ArticleList, ArticleTree, ChatPanel)
- Convert supporting components
- Convert page components

**Phase 6: Router and Main**
- Convert router configuration
- Convert main.js to main.ts
- Update App.vue

**Phase 7: Testing and Validation**
- Add property-based tests
- Add unit tests for type guards
- Validate type coverage
- Performance testing

### TypeScript Configuration

**tsconfig.json**:
```json
{
  "extends": "@vue/tsconfig/tsconfig.dom.json",
  "compilerOptions": {
    "target": "ES2020",
    "module": "ESNext",
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "moduleResolution": "bundler",
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true,
    "skipLibCheck": true,
    "esModuleInterop": true,
    "allowSyntheticDefaultImports": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    "jsx": "preserve",
    "baseUrl": ".",
    "paths": {
      "@/*": ["./Vue/*"]
    },
    "types": ["vite/client", "vitest/globals"]
  },
  "include": [
    "Vue/**/*.ts",
    "Vue/**/*.d.ts",
    "Vue/**/*.tsx",
    "Vue/**/*.vue"
  ],
  "exclude": [
    "node_modules",
    "wwwroot"
  ]
}
```

### Package Dependencies

Additional packages needed:
```json
{
  "dependencies": {
    "vue": "^3.4.0",
    "@microsoft/signalr": "^8.0.0"
  },
  "devDependencies": {
    "@vitejs/plugin-vue": "^6.0.3",
    "@vue/tsconfig": "^0.5.0",
    "typescript": "^5.3.0",
    "vite": "^7.3.1",
    "vitest": "^4.0.17",
    "fast-check": "^3.15.0",
    "@types/node": "^20.11.0"
  }
}
```

### Vite Configuration Update

**vite.config.ts**:
```typescript
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'path';

export default defineConfig({
  plugins: [
    vue({
      template: {
        compilerOptions: {
          isCustomElement: (tag) => tag === 'json-viewer'
        }
      }
    })
  ],
  
  base: '/dist/',
  
  build: {
    outDir: 'wwwroot/dist',
    emptyOutDir: true,
    
    rollupOptions: {
      input: {
        main: resolve(__dirname, 'Vue/main.ts')
      },
      output: {
        entryFileNames: 'js/[name].js',
        chunkFileNames: 'js/[name]-[hash].js',
        assetFileNames: (assetInfo) => {
          if (assetInfo.name?.endsWith('.css')) {
            return 'css/[name].[ext]';
          }
          return 'assets/[name]-[hash].[ext]';
        }
      }
    },
    
    sourcemap: true,
    minify: 'terser',
    chunkSizeWarningLimit: 1000
  },
  
  resolve: {
    alias: {
      '@': resolve(__dirname, 'Vue')
    }
  },
  
  // Enable TypeScript type checking
  esbuild: {
    tsconfigRaw: {
      compilerOptions: {
        experimentalDecorators: true
      }
    }
  }
});
```

### Best Practices

1. **Strict Mode**: Use TypeScript strict mode for maximum type safety
2. **Explicit Types**: Prefer explicit type annotations over inference for public APIs
3. **Type Guards**: Implement type guards for external data (API responses, SignalR messages)
4. **Generic Constraints**: Use generic constraints to ensure type safety in reusable functions
5. **Readonly Props**: Mark component props as readonly to prevent mutations
6. **Discriminated Unions**: Use discriminated unions for state machines and variants
7. **Avoid Any**: Never use `any` type; use `unknown` and type guards instead
8. **Null Safety**: Use strict null checks and handle null/undefined explicitly
9. **Type Imports**: Use `import type` for type-only imports to improve tree-shaking
10. **Documentation**: Document complex types with JSDoc comments

### Performance Considerations

1. **Build Time**: TypeScript compilation adds build time; use incremental compilation
2. **Bundle Size**: Type definitions don't affect bundle size (stripped at build time)
3. **Runtime Validation**: Type guards add minimal runtime overhead; use selectively
4. **Development Experience**: TypeScript provides better IDE performance through IntelliSense
5. **Hot Module Replacement**: Vite's HMR works seamlessly with TypeScript

### Backward Compatibility

During migration:
- JavaScript files can import TypeScript files
- TypeScript files can import JavaScript files (with implicit any)
- Existing functionality remains unchanged
- No breaking changes to component APIs
- Gradual adoption allows testing at each step
- Commit after each major task is complete.