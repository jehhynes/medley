# Solution Architecture Document

**Project:** Medley
**Date:** 2025-10-17
**Author:** Medley Developer

## Executive Summary

Medley is an AI-Powered Product Intelligence Platform that transforms organizational conversations into structured documentation through human-AI collaboration. The system addresses the fundamental paradox that developers hate writing documentation but desperately need it.

**Architecture Overview:** Clean Architecture monolith using ASP.NET Core MVC with server-side rendering and abstracted external dependencies. The system processes meeting transcripts, extracts insights using AI services, clusters related content, and generates comprehensive documentation through a transparent human-AI review process. Database, ORM, AI Processing, and File Storage are abstracted through interfaces for easy mocking and implementation swapping.

**Key Design Principles:** Transparency-first AI interactions, efficiency for time-constrained teams, human-AI partnership model, and context-aware intelligence. The architecture supports sub-2-second response times, 10,000+ fragments/hour processing, and 99.9% uptime SLA.

## 1. Technology Stack and Decisions

### 1.1 Technology and Library Decision Table

| Category | Technology (Default) | Version | Interface Abstraction | Justification |
|----------|---------------------|---------|----------------------|---------------|
| Framework | ASP.NET Core MVC | 8.0 | Built-in DI | Server-side rendering, mature ecosystem, Clean Architecture support |
| Language | C# | 12.0 | .NET Standard | Type safety, performance, team expertise, .NET ecosystem |
| Database | PostgreSQL | 16.0+ | `IDbContextFactory<T>` | ACID compliance, pgvector extension, easily swappable for SQL Server/MySQL |
| ORM | Entity Framework Core | 8.0+ | `IRepository<T>`, `IUnitOfWork` | Code-first migrations, LINQ support, easily swappable for NHibernate/Dapper |
| Authentication | ASP.NET Core Identity | 8.0 | `IUserManager`, `ISignInManager` | Built-in security, role management, extensible user model |
| AI Processing | AWS Bedrock (Claude 4.5) | Latest | `IAiProcessingService` | Advanced reasoning, large context, easily swappable for OpenAI/OpenRouter |
| Background Jobs | Hangfire | 1.8.6 | `IBackgroundJobService` | Reliable job processing, dashboard, retry logic, persistence |
| Real-time | SignalR | 8.0 | `INotificationService` | Native .NET integration, automatic fallbacks, scalable |
| Caching | Redis | 7.2 | `IDistributedCache` | Distributed caching, session storage, high performance |
| File Storage | AWS S3 | Latest | `IFileStorageService` | Scalable object storage, easily swappable for Azure Blob/Local FileSystem |
| Styling | Bootstrap | 5.3 | CSS framework agnostic | Auto dark/light mode, responsive grid, accessibility |
| Testing | xUnit + Moq | Latest | Standard .NET testing | .NET standard, mocking support, parallel execution |
| Integration | Fellow.ai API | v1 | `IMeetingDataService` | Primary data source, meeting transcript access |
| Hosting | Self-hosted IIS | Latest | `IHostingService` | Full control, Windows integration, familiar deployment |

## 2. Application Architecture

### 2.1 Architecture Pattern

**Clean Architecture Monolith** with clear layer separation and dependency inversion:

```
Presentation Layer (Web/UI)
├── Controllers, Views, Razor Pages
├── ASP.NET Core Identity (UI components)
└── → Application Layer

Application Layer (Business Logic)
├── Services (UserService, FragmentService, DocumentService)
├── Interfaces (IRepository<T>, IUnitOfWork, IAiProcessingService, 
│   IFileStorageService, IMeetingDataService, IDbContextFactory<T>)
└── → Domain Layer

Domain Layer (Core Business Logic)
├── Entities (User, Fragment, Article, Cluster, etc.)
├── Value Objects
├── Domain Services (if needed)
└── No dependencies (center of architecture)

Infrastructure Layer (External Concerns)
├── Data Access (EF Core, Repositories, Identity Store)
├── External Services (Fellow.ai, AWS Bedrock, S3)
├── Cross-Cutting (Hangfire, Redis, SignalR, Logging)
└── → Application Layer (implements interfaces)
```

**Key Principles:**
- All dependencies point inward to Domain layer
- Direct entity usage (no DTOs between layers)
- Simplified repository pattern with generic base classes
- Unit of Work pattern for transaction management
- **Interface abstractions** for Database, ORM, AI Processing, and File Storage
- **Easy mocking** for unit tests and **implementation swapping** without code changes

### 2.1.1 Key Interface Abstractions

**Database & ORM Abstraction:**
```csharp
// Allows swapping PostgreSQL ↔ SQL Server ↔ MySQL
public interface IDbContextFactory<T> where T : DbContext
{
    T CreateDbContext();
}

// Allows swapping Entity Framework ↔ NHibernate ↔ Dapper
public interface IRepository<T> where T : class
{
    Task<T> GetAsync(int id);
    Task<IEnumerable<T>> GetMultipleAsync(IEnumerable<int> ids);
    IQueryable<T> Query();
    Task SaveAsync(T entity);
}
```

**AI Processing Abstraction:**
```csharp
// Allows swapping AWS Bedrock ↔ OpenAI ↔ Azure OpenAI ↔ OpenRouter
public interface IAiProcessingService
{
    Task<FragmentExtractionResult> ExtractFragmentsAsync(string transcript);
    Task<DocumentGenerationResult> GenerateDocumentAsync(IEnumerable<Fragment> fragments);
    Task<ClusteringResult> ClusterFragmentsAsync(IEnumerable<Fragment> fragments);
}
```

**File Storage Abstraction:**
```csharp
// Allows swapping AWS S3 ↔ Azure Blob Storage ↔ Local FileSystem
public interface IFileStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType);
    Task<Stream> DownloadAsync(string fileKey);
    Task<bool> DeleteAsync(string fileKey);
    Task<string> GetDownloadUrlAsync(string fileKey, TimeSpan expiry);
}
```

**Benefits:**
- **Easy Testing:** Mock interfaces for unit tests
- **Implementation Swapping:** Change providers via DI configuration
- **Development Flexibility:** Use local implementations for development
- **Vendor Independence:** Not locked into specific cloud providers

### 2.2 Server-Side Rendering Strategy

**ASP.NET Core MVC with Razor Pages** for optimal performance and SEO:
- Server-side rendering for all pages (no SPA complexity)
- Razor Pages for simple CRUD operations
- MVC Controllers for complex workflows
- Partial views for reusable components
- HTMX for dynamic interactions without full page reloads

### 2.3 Page Routing and Navigation

**Conventional routing** with area-based organization:
- `/` - Dashboard and overview
- `/Integrations` - Fellow.ai and external service management
- `/Fragments` - Fragment explorer and search
- `/Articles` - Article generation and management
- `/Review` - AI content review interface
- `/Analytics` - Usage and performance metrics

**Sidebar-first navigation** with Bootstrap responsive design and progressive disclosure.

### 2.4 Data Fetching Approach

**Server-side data access** through service layer:
- Controllers call Application Services
- Services coordinate Repository operations
- Repositories handle Entity Framework queries
- Background jobs for AI processing via Hangfire
- Real-time updates via SignalR hubs

## 3. Data Architecture

### 3.1 Database Schema

{{database_schema}}

### 3.2 Data Models and Relationships

{{data_models}}

### 3.3 Data Migrations Strategy

{{migrations_strategy}}

## 4. MVC Controllers and Views

### 4.1 Controller Structure

**Server-rendered MVC controllers** handling user interactions:

**HomeController:**
- `GET /` - Dashboard with system overview
- `GET /Health` - Application health status

**AuthController:**
- `GET /Auth/Login` - Login page
- `POST /Auth/Login` - Process login
- `GET /Auth/Register` - Registration page  
- `POST /Auth/Register` - Process registration
- `POST /Auth/Logout` - User logout

**IntegrationController:**
- `GET /Integrations` - Integration management page
- `GET /Integrations/Create` - Add integration form
- `POST /Integrations/Create` - Process new integration
- `GET /Integrations/Edit/{id}` - Edit integration form
- `POST /Integrations/Edit/{id}` - Update integration
- `POST /Integrations/Delete/{id}` - Delete integration
- `POST /Integrations/Test/{id}` - Test connection

**FragmentController:**
- `GET /Fragments` - Fragment search and browse
- `GET /Fragments/Details/{id}` - Fragment details view
- `GET /Fragments/Search` - Advanced search form
- `POST /Fragments/Search` - Process search query

**ArticleController:**
- `GET /Articles` - Article library
- `GET /Articles/Generate` - Article generation form
- `POST /Articles/Generate` - Create new article
- `GET /Articles/Review/{id}` - Review interface
- `POST /Articles/Review/{id}` - Submit review
- `GET /Articles/Export/{id}` - Export article

### 4.2 View Structure

**Shared Layout:**
- `_Layout.cshtml` - Main layout with navigation
- `_LoginPartial.cshtml` - Authentication status
- `_ValidationScriptsPartial.cshtml` - Client validation

**Page-Specific Views:**
- Razor views for each controller action
- Partial views for reusable components
- ViewModels for data binding and validation

### 4.3 Form Handling

**Server-side form processing** with model binding:
- ASP.NET Core model validation
- Anti-forgery token protection
- File upload handling for document imports
- Background job triggering for AI processing

## 5. Authentication and Authorization

### 5.1 Auth Strategy

{{auth_strategy}}

### 5.2 Session Management

{{session_management}}

### 5.3 Protected Routes

{{protected_routes}}

### 5.4 Role-Based Access Control

{{rbac}}

## 6. State Management

### 6.1 Server State

{{server_state}}

### 6.2 Client State

{{client_state}}

### 6.3 Form State

{{form_state}}

### 6.4 Caching Strategy

{{caching_strategy}}

## 7. UI/UX Architecture

### 7.1 Component Structure

{{component_structure}}

### 7.2 Styling Approach

{{styling_approach}}

### 7.3 Responsive Design

{{responsive_design}}

### 7.4 Accessibility

{{accessibility}}

## 8. Performance Optimization

### 8.1 SSR Caching

{{ssr_caching}}

### 8.2 Static Generation

{{static_generation}}

### 8.3 Image Optimization

{{image_optimization}}

### 8.4 Code Splitting

{{code_splitting}}

## 9. SEO and Meta Tags

### 9.1 Meta Tag Strategy

{{meta_tag_strategy}}

### 9.2 Sitemap

{{sitemap}}

### 9.3 Structured Data

{{structured_data}}

## 10. Deployment Architecture

### 10.1 Hosting Platform

{{hosting_platform}}

### 10.2 CDN Strategy

{{cdn_strategy}}

### 10.3 Edge Functions

{{edge_functions}}

### 10.4 Environment Configuration

{{environment_config}}

## 11. Component and Integration Overview

### 11.1 Major Modules

{{major_modules}}

### 11.2 Page Structure

{{page_structure}}

### 11.3 Shared Components

{{shared_components}}

### 11.4 Third-Party Integrations

{{third_party_integrations}}

## 12. Architecture Decision Records

{{architecture_decisions}}

**Key decisions:**

- Why this framework? {{framework_decision}}
- SSR vs SSG? {{ssr_vs_ssg_decision}}
- Database choice? {{database_decision}}
- Hosting platform? {{hosting_decision}}

## 13. Implementation Guidance

### 13.1 Development Workflow

{{development_workflow}}

### 13.2 File Organization

{{file_organization}}

### 13.3 Naming Conventions

{{naming_conventions}}

### 13.4 Best Practices

{{best_practices}}

## 14. Proposed Source Tree

```
medley/
├── src/
│   ├── Medley.Domain/
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Organization.cs
│   │   │   ├── Integration.cs
│   │   │   ├── Fragment.cs
│   │   │   └── Document.cs
│   │   ├── Enums/
│   │   │   ├── IntegrationType.cs
│   │   │   ├── FragmentType.cs
│   │   │   └── ArticleStatus.cs
│   │   └── ValueObjects/
│   ├── Medley.Application/
│   │   ├── Services/
│   │   │   ├── UserService.cs
│   │   │   ├── FragmentService.cs
│   │   │   └── DocumentService.cs
│   │   ├── Interfaces/
│   │   │   ├── IRepository.cs
│   │   │   ├── IAiProcessingService.cs
│   │   │   ├── IFileStorageService.cs
│   │   │   └── IMeetingDataService.cs
│   │   └── Models/
│   ├── Medley.Infrastructure/
│   │   ├── Data/
│   │   │   ├── MedleyDbContext.cs
│   │   │   ├── Repositories/
│   │   │   └── Migrations/
│   │   ├── Services/
│   │   │   ├── BedrockAiService.cs
│   │   │   ├── S3FileStorageService.cs
│   │   │   ├── FellowAiService.cs
│   │   │   └── GitHubService.cs
│   │   └── Configuration/
│   ├── Medley.Web/
│   │   ├── Controllers/
│   │   │   ├── HomeController.cs
│   │   │   ├── AuthController.cs
│   │   │   ├── IntegrationController.cs
│   │   │   ├── FragmentController.cs
│   │   │   └── DocumentController.cs
│   │   ├── Views/
│   │   │   ├── Shared/
│   │   │   ├── Home/
│   │   │   ├── Integration/
│   │   │   ├── Fragment/
│   │   │   └── Document/
│   │   ├── Models/
│   │   ├── wwwroot/
│   │   │   ├── css/
│   │   │   ├── js/
│   │   │   └── lib/
│   │   ├── Program.cs
│   │   └── appsettings.json
|   ├── tests/
│   │   ├── Medley.Domain.Tests/
│   │   ├── Medley.Application.Tests/
│   │   ├── Medley.Infrastructure.Tests/
│   │   └── Medley.Web.Tests/
├───|── Medley.sln
├── docs/
│   ├── solution-architecture.md
│   ├── tech-spec-epic-1.md
│   ├── tech-spec-epic-2.md
│   ├── tech-spec-epic-3.md
│   ├── tech-spec-epic-4.md
│   ├── tech-spec-epic-5.md
│   ├── PRD.md
│   ├── epics.md
│   └── ux-specification.md
├── .github/
│   └── workflows/
│       └── ci-cd.yml
└── README.md
```

**Critical folders:**

- **src/Medley.Domain/**: Core business entities and logic with no external dependencies
- **src/Medley.Application/**: Business services and interface abstractions for external dependencies  
- **src/Medley.Infrastructure/**: External service implementations (database, AI, file storage, APIs)

## 15. Testing Strategy

### 15.1 Unit Tests

**Domain Layer Testing:**
- Pure unit tests with no dependencies
- Test entities, value objects, and domain services
- Focus on business logic validation

**Application Layer Testing:**
- Mock all interface dependencies (`IAiProcessingService`, `IFileStorageService`, etc.)
- Test service orchestration and business workflows
- Verify proper repository and external service coordination

**Infrastructure Layer Testing:**
- Test repository implementations against real database (integration tests)
- Test external service integrations with test doubles
- Verify interface contract compliance

### 15.2 Integration Tests

**Database Integration:**
- Test repository implementations with real PostgreSQL
- Verify Entity Framework mappings and migrations
- Test complex queries and relationships

**External Service Integration:**
- Test AI processing service with real/mock AI providers
- Test file storage service with real/mock storage providers
- Test meeting data service with Fellow.ai test environment

### 15.3 E2E Tests

**Critical User Journeys:**
- Complete document generation workflow (upload → process → review → publish)
- Fragment clustering and similarity detection
- User authentication and authorization flows
- Real-time notification delivery via SignalR

### 15.4 Coverage Goals

- **Unit Tests:** 90% code coverage for Domain and Application layers
- **Integration Tests:** 80% coverage for Infrastructure layer
- **E2E Tests:** 100% coverage of critical user journeys
- **Interface Mocking:** All external dependencies mockable for isolated testing

## 16. DevOps and CI/CD

### 16.1 Deployment Strategy

**Self-Hosted IIS Deployment:**
- Windows Server with IIS hosting
- .NET 8 runtime installation
- Application pool configuration for ASP.NET Core
- Environment-specific configuration management

### 16.2 CI/CD Pipeline

**Build Pipeline:**
- Automated builds on code commits
- Unit and integration test execution
- Code quality analysis and coverage reporting
- NuGet package restoration and dependency management

**Deployment Pipeline:**
- Automated deployment to staging environment
- Smoke tests and health checks
- Manual approval gate for production deployment
- Blue-green deployment strategy for zero downtime

### 16.3 Environment Management

**Configuration Management:**
- Environment-specific appsettings.json files
- Secure storage of connection strings and API keys
- Provider switching via configuration (Database, AI, File Storage)
- Health check endpoints for monitoring

### 16.4 Monitoring and Logging

**Application Monitoring:**
- Structured logging with Serilog
- Performance counters and metrics collection
- Error tracking and alerting
- Background job monitoring via Hangfire dashboard

## 17. Security

### 17.1 Authentication and Authorization

**ASP.NET Core Identity:**
- Secure user registration and login
- Password hashing with PBKDF2
- Role-based access control (Admin, User roles)
- Session management with secure cookies

### 17.2 Data Protection

**Data at Rest:**
- Database encryption using PostgreSQL built-in encryption
- Secure configuration storage (connection strings, API keys)
- File storage encryption via AWS S3 server-side encryption

**Data in Transit:**
- HTTPS enforcement for all communications
- TLS 1.2+ for external API communications
- Secure WebSocket connections for SignalR

### 17.3 API Security

**External Integrations:**
- OAuth 2.0 for future API authentication
- API keys for Fellow.ai integration
- API key management for AWS Bedrock and S3
- Rate limiting and request throttling
- Input validation and sanitization

### 17.4 Application Security

**Web Application Security:**
- CSRF protection via ASP.NET Core anti-forgery tokens
- XSS prevention through Razor view encoding
- SQL injection prevention via Entity Framework parameterized queries
- Content Security Policy (CSP) headers

---

## Specialist Sections

{{specialist_sections_summary}}

---

_Generated using BMad Method Solution Architecture workflow_