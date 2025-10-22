# Story 2.1: Integration Management Interface

Status: Ready

## Story

As a system administrator,
I want to configure connections to external tools through a web interface,
So that I can easily manage data sources without technical configuration.

## Acceptance Criteria

1. Integration management page with add/edit/delete functionality for connections
2. Form validation for integration configuration fields (API keys, URLs, scopes)
3. Integration status indicators (connected, error, disconnected) with real-time updates
4. Basic error handling and user feedback for configuration issues
5. Integration list view with search and filtering capabilities
6. Role-based access control (Admin only for integration management)
7. Integration health monitoring with automatic status checks

## Tasks / Subtasks

- [ ] Task 1: Create IntegrationController and Views (AC: 1, 2, 4, 5)
  - [ ] Subtask 1.1: Create IntegrationController with CRUD action methods (Index, Create, Edit, Delete)
  - [ ] Subtask 1.2: Create Integration/Index.cshtml view with list display and search/filter UI
  - [ ] Subtask 1.3: Create Integration/Create.cshtml view with form for new integrations
  - [ ] Subtask 1.4: Create Integration/Edit.cshtml view with form for editing integrations
  - [ ] Subtask 1.5: Implement form validation using ASP.NET Core model validation
  - [ ] Subtask 1.6: Add client-side validation with jQuery Unobtrusive Validation
  - [ ] Subtask 1.7: Implement error handling and user-friendly error messages
  - [ ] Subtask 1.8: Add search and filtering functionality to Index view

- [ ] Task 2: Implement Integration Service Layer (AC: 1, 4, 7)
  - [ ] Subtask 2.1: Create IIntegrationService interface in Application layer
  - [ ] Subtask 2.2: Implement IntegrationService with CRUD operations
  - [ ] Subtask 2.3: Add integration validation logic (API key format, URL validation)
  - [ ] Subtask 2.4: Implement connection testing functionality
  - [ ] Subtask 2.5: Add error handling and logging for integration operations
  - [ ] Subtask 2.6: Implement integration health check logic
  - [ ] Subtask 2.7: Register service in DI container

- [ ] Task 3: Add Real-time Status Updates with SignalR (AC: 3, 7)
  - [ ] Subtask 3.1: Create IntegrationStatusHub SignalR hub
  - [ ] Subtask 3.2: Implement status broadcasting from service layer
  - [ ] Subtask 3.3: Add JavaScript client code for receiving status updates
  - [ ] Subtask 3.4: Update UI dynamically when status changes
  - [ ] Subtask 3.5: Add connection state indicators (connected, error, disconnected)
  - [ ] Subtask 3.6: Implement automatic reconnection logic

- [ ] Task 4: Implement Role-Based Access Control (AC: 6)
  - [ ] Subtask 4.1: Add [Authorize(Roles = "Admin")] attribute to IntegrationController
  - [ ] Subtask 4.2: Create access denied view for non-admin users
  - [ ] Subtask 4.3: Hide integration management menu for non-admin users
  - [ ] Subtask 4.4: Add role-based UI element visibility in views
  - [ ] Subtask 4.5: Test access control with different user roles

- [ ] Task 5: Add Integration Status Monitoring (AC: 3, 7)
  - [ ] Subtask 5.1: Create background job for periodic health checks using Hangfire
  - [ ] Subtask 5.2: Implement health check logic for each integration type
  - [ ] Subtask 5.3: Update integration status in database based on health check results
  - [ ] Subtask 5.4: Trigger SignalR notifications on status changes
  - [ ] Subtask 5.5: Add manual "Test Connection" button to UI
  - [ ] Subtask 5.6: Display last health check timestamp in UI

- [ ] Task 6: Testing and Validation (AC: All)
  - [ ] Subtask 6.1: Write unit tests for IntegrationService
  - [ ] Subtask 6.2: Write integration tests for IntegrationController
  - [ ] Subtask 6.3: Test form validation with invalid inputs
  - [ ] Subtask 6.4: Test real-time status updates with SignalR
  - [ ] Subtask 6.5: Test role-based access control
  - [ ] Subtask 6.6: Test search and filtering functionality
  - [ ] Subtask 6.7: Perform accessibility testing (WCAG AA compliance)

## Dev Notes

### Architecture Patterns and Constraints

**Clean Architecture Compliance:**
- IntegrationController in Presentation layer calls IIntegrationService
- IntegrationService in Application layer coordinates Repository operations
- Integration entity in Domain layer with no external dependencies
- Infrastructure layer implements external service connections

**Interface Abstractions:**
- `IIntegrationService` - Application service for integration management
- `IRepository<Integration>` - Data access abstraction
- `INotificationService` - SignalR abstraction for real-time updates
- `IBackgroundJobService` - Hangfire abstraction for health checks

**Technology Stack:**
- ASP.NET Core MVC 9.0 with Razor views for server-side rendering
- Bootstrap 5.3 for responsive UI with auto dark/light mode
- SignalR for real-time status updates
- Hangfire for background health check jobs
- Entity Framework Core for data persistence

### Source Tree Components to Touch

**New Files to Create:**
```
src/Medley.Application/Services/IntegrationService.cs
src/Medley.Application/Interfaces/IIntegrationService.cs
src/Medley.Web/Controllers/IntegrationController.cs
src/Medley.Web/Views/Integration/Index.cshtml
src/Medley.Web/Views/Integration/Create.cshtml
src/Medley.Web/Views/Integration/Edit.cshtml
src/Medley.Web/Views/Integration/_IntegrationStatusPartial.cshtml
src/Medley.Web/Hubs/IntegrationStatusHub.cs
src/Medley.Web/wwwroot/js/integration-status.js
tests/Medley.Application.Tests/Services/IntegrationServiceTests.cs
tests/Medley.Web.Tests/Controllers/IntegrationControllerTests.cs
```

**Existing Files to Modify:**
```
src/Medley.Web/Program.cs - Register IntegrationService and SignalR hub
src/Medley.Web/Views/Shared/_Layout.cshtml - Add integration management menu item
src/Medley.Domain/Entities/Integration.cs - Already exists from Story 1.4
```

### Testing Standards Summary

**Unit Tests (xUnit + Moq):**
- Mock IRepository<Integration> for service tests
- Mock IIntegrationService for controller tests
- Test validation logic with invalid inputs
- Test error handling scenarios

**Integration Tests:**
- Test controller actions with real database context
- Test SignalR hub message broadcasting
- Test background job execution with Hangfire

**Accessibility Testing:**
- WCAG AA compliance for all form elements
- Keyboard navigation support
- Screen reader compatibility
- Sufficient color contrast for status indicators

### Project Structure Notes

**Alignment with Unified Project Structure:**
- Controllers follow MVC pattern in `src/Medley.Web/Controllers/`
- Views organized by controller in `src/Medley.Web/Views/Integration/`
- Services in Application layer `src/Medley.Application/Services/`
- SignalR hubs in `src/Medley.Web/Hubs/`
- JavaScript files in `src/Medley.Web/wwwroot/js/`

**No Conflicts Detected:**
- Integration entity already defined in Story 1.4
- SignalR already configured in Story 1.8
- Hangfire already configured in Story 1.5
- Bootstrap UI framework already established in Story 1.8

### References

**Source Documents:**
- [Source: docs/epics.md#Story 2.1] - User story and acceptance criteria
- [Source: docs/tech-spec-epic-2.md#MVC Controllers and Views] - Controller structure and view requirements
- [Source: docs/tech-spec-epic-2.md#Service Interfaces] - Service interface definitions
- [Source: docs/solution-architecture.md#Application Architecture] - Clean Architecture pattern
- [Source: docs/solution-architecture.md#UI/UX Architecture] - Bootstrap and responsive design
- [Source: docs/tech-spec-epic-1.md#SignalR Configuration] - Real-time updates implementation
- [Source: docs/tech-spec-epic-1.md#Hangfire Configuration] - Background job processing

**Key Technical Details:**
- Integration entity schema defined in Story 1.4 with IntegrationType enum, ApiKey, BaseUrl, Status fields
- SignalR configured in Story 1.8 for real-time notifications
- Hangfire configured in Story 1.5 for background processing
- Role-based authorization using ASP.NET Core Identity from Story 1.2
- Bootstrap 5.3 UI framework from Story 1.8 with responsive design

## Dev Agent Record

### Context Reference

- [Story Context XML](story-context-2.1.xml) - Generated 2025-10-22

### Agent Model Used

<!-- To be filled by dev agent -->

### Debug Log References

<!-- To be filled by dev agent -->

### Completion Notes List

<!-- To be filled by dev agent -->

### File List

<!-- To be filled by dev agent -->
