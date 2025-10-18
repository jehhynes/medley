# Tech Spec: Epic 1 - Foundation & Core Infrastructure

**Epic:** Foundation & Core Infrastructure  
**Date:** 2025-10-17  
**Author:** Medley Developer  
**Architecture Reference:** [Solution Architecture](./solution-architecture.md)

## Epic Overview

Establish the foundational infrastructure for the AI-Powered Product Intelligence Platform, including project setup, authentication system, vector database, core data models, background processing, and basic UI framework.

**Value Delivery:** Functional platform foundation with user management and system health monitoring.

## Stories Included

- Story 1.1: Project Setup and Development Environment
- Story 1.2: User Authentication and Authorization System  
- Story 1.3: Vector Database Setup with pgvector
- Story 1.4: Core Data Models and Database Schema
- Story 1.5: Background Processing Infrastructure
- Story 1.6: AWS Integration Setup
- Story 1.7: CI/CD Pipeline Foundation
- Story 1.8: Basic UI Framework and Navigation

## Architecture Extract

### Technology Stack (Epic 1 Specific)

| Component | Technology | Interface | Justification |
|-----------|------------|-----------|---------------|
| Framework | ASP.NET Core MVC 8.0 | Built-in DI | Clean Architecture support, SSR |
| Database | PostgreSQL 16.0+ | `IDbContextFactory<T>` | pgvector extension, ACID compliance |
| ORM | Entity Framework Core 8.0+ | `IRepository<T>` | Code-first migrations, LINQ |
| Authentication | ASP.NET Core Identity 8.0 | `IUserManager` | Built-in security, role management |
| Background Jobs | ASP.NET Hosted Services | `IBackgroundJobService` | Native .NET integration |
| File Storage | AWS S3 | `IFileStorageService` | Scalable object storage |
| UI Framework | Bootstrap 5.3 | CSS framework agnostic | Responsive, accessibility |

### Data Models (Epic 1)

**Core Entities:**
```csharp
// Domain Layer Entities
public class User : BaseEntity
{
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; }
    public ICollection<UserRole> UserRoles { get; set; }
}

public class Organization : BaseEntity
{
    public string Name { get; set; }
    public string Domain { get; set; }
    public ICollection<User> Users { get; set; }
    public ICollection<Integration> Integrations { get; set; }
}

public class Integration : BaseEntity
{
    public string Name { get; set; }
    public IntegrationType Type { get; set; } // Fellow, GitHub, etc.
    public string Configuration { get; set; } // JSON config
    public IntegrationStatus Status { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; }
}

public class Source : BaseEntity
{
    public string Name { get; set; }
    public SourceType Type { get; set; } // Meeting, Commit, Issue, etc.
    public string ExternalId { get; set; }
    public string Metadata { get; set; } // JSON metadata
    public int IntegrationId { get; set; }
    public Integration Integration { get; set; }
}

// Prepared for future epics
public class Fragment : BaseEntity
{
    public string Content { get; set; }
    public FragmentType Type { get; set; }
    public float ConfidenceScore { get; set; }
    public Vector Embedding { get; set; } // pgvector
    public int SourceId { get; set; }
    public Source Source { get; set; }
}
```

### Component Architecture

**Clean Architecture Layers:**
```
Presentation Layer (Medley.Web)
├── Controllers (AuthController, HomeController, IntegrationController)
├── Views (Razor pages and layouts)
├── Models (ViewModels for UI binding)
└── → Application Layer

Application Layer (Medley.Application)
├── Services (UserService, OrganizationService, IntegrationService)
├── Interfaces (IRepository<T>, IUnitOfWork, IFileStorageService)
└── → Domain Layer

Domain Layer (Medley.Domain)
├── Entities (User, Organization, Integration, Source)
├── Enums (IntegrationType, SourceType, etc.)
└── No dependencies

Infrastructure Layer (Medley.Infrastructure)
├── Data (EF Core DbContext, Repository implementations)
├── Services (S3FileStorageService, BackgroundJobService)
└── → Application Layer (implements interfaces)
```

### MVC Controllers and Views (Epic 1)

**AuthController:**
- `GET /Auth/Login` - Login page with form
- `POST /Auth/Login` - Process login credentials
- `GET /Auth/Register` - Registration page
- `POST /Auth/Register` - Process user registration
- `POST /Auth/Logout` - User logout action

**HomeController:**
- `GET /` - Dashboard with system status overview
- `GET /Health` - Health check page for monitoring

**IntegrationController:**
- `GET /Integrations` - Integration management page
- `GET /Integrations/Create` - Add new integration form
- `POST /Integrations/Create` - Process integration creation
- `GET /Integrations/Edit/{id}` - Edit integration form
- `POST /Integrations/Edit/{id}` - Update integration settings

**Views and Forms:**
- Server-rendered Razor views with Bootstrap styling
- Form validation using ASP.NET Core model validation
- Anti-forgery tokens for security
- Responsive design for mobile compatibility

## Implementation Guidance

### Development Workflow

1. **Project Setup (Story 1.1)**
   - Create ASP.NET Core MVC project with Clean Architecture structure
   - Configure PostgreSQL connection with pgvector extension
   - Set up Entity Framework Core with initial migration
   - Implement health check endpoints

2. **Authentication (Story 1.2)**
   - Configure ASP.NET Core Identity
   - Implement role-based authorization
   - Create user management interfaces
   - Add audit logging for auth events

3. **Data Foundation (Stories 1.3-1.4)**
   - Set up pgvector extension and vector operations
   - Create all domain entities and relationships
   - Implement repository pattern with generic base classes
   - Create database migrations and seed data

4. **Background Processing (Story 1.5)**
   - Implement ASP.NET Hosted Services for background jobs
   - Create job queue and status tracking
   - Add error handling and retry logic

5. **AWS Integration (Story 1.6)**
   - Configure AWS SDK and credentials
   - Set up S3 bucket and Bedrock access
   - Implement file storage service abstraction
   - Add AWS health checks

6. **CI/CD (Story 1.7)**
   - Set up GitHub Actions workflow
   - Configure automated testing and deployment
   - Add security scanning and code quality checks

7. **UI Framework (Story 1.8)**
   - Implement Bootstrap-based responsive UI
   - Create main navigation and dashboard
   - Add user profile management
   - Ensure accessibility compliance

### File Organization

```
src/
├── Medley.Domain/
│   ├── Entities/
│   ├── Enums/
│   └── ValueObjects/
├── Medley.Application/
│   ├── Services/
│   ├── Interfaces/
│   └── Models/
├── Medley.Infrastructure/
│   ├── Data/
│   ├── Services/
│   └── Configuration/
├── Medley.Web/
│   ├── Controllers/
│   ├── Views/
│   ├── Models/
│   └── wwwroot/
└── tests/
    ├── Medley.Domain.Tests/
    ├── Medley.Application.Tests/
    ├── Medley.Infrastructure.Tests/
    └── Medley.Web.Tests/
```

### Testing Approach

**Unit Tests:**
- Domain entities and business logic
- Application services with mocked dependencies
- Repository pattern validation

**Integration Tests:**
- Database operations with real PostgreSQL
- AWS service integrations
- Authentication and authorization flows

**Key Interfaces to Mock:**
- `IRepository<T>` for data access
- `IFileStorageService` for AWS S3
- `IBackgroundJobService` for job processing

### Configuration Management

**Development Environment:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=medley_dev;Username=dev;Password=dev"
  },
  "AWS": {
    "Region": "us-east-1",
    "S3BucketName": "medley-dev-storage"
  },
  "Providers": {
    "Database": "PostgreSQL",
    "FileStorage": "LocalFileSystem",
    "BackgroundJobs": "HostedServices"
  }
}
```

**Production Environment:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "[Production PostgreSQL Connection]"
  },
  "AWS": {
    "Region": "us-east-1", 
    "S3BucketName": "medley-prod-storage"
  },
  "Providers": {
    "Database": "PostgreSQL",
    "FileStorage": "S3",
    "BackgroundJobs": "HostedServices"
  }
}
```

## Success Criteria

**Epic 1 Complete When:**
- ✅ ASP.NET Core project running with Clean Architecture
- ✅ User authentication and role management working
- ✅ PostgreSQL database with pgvector extension operational
- ✅ All core data models implemented and migrated
- ✅ Background job processing infrastructure ready
- ✅ AWS S3 and Bedrock integration configured
- ✅ CI/CD pipeline deploying successfully
- ✅ Responsive UI framework with navigation complete
- ✅ Health checks and monitoring operational
- ✅ All unit and integration tests passing

**Ready for Epic 2:** Fellow.ai Integration & Fragment Processing