# Story 1.8: Basic UI Framework and Navigation

Status: Ready

## Story

As a user,
I want to access a clean, responsive web interface with basic navigation,
So that I can interact with the platform effectively across different devices.

## Acceptance Criteria

1. Responsive Bootstrap-based UI framework implemented
2. Main navigation menu with placeholder sections for all major features
3. Dashboard page with system status and recent activity overview
4. User profile management interface implemented
5. Basic error handling and user-friendly error pages created
6. Accessibility compliance (WCAG AA) implemented for core UI components
7. Mobile-responsive design tested across common device sizes
8. Authentication-aware navigation (login/logout, role-based menu items)

## Tasks / Subtasks

- [x] Task 1: Bootstrap UI Framework Setup (AC: 1)
  - [x] 1.1: Install Bootstrap 5.3 via NuGet or CDN
  - [x] 1.2: Configure Bootstrap in _Layout.cshtml with proper CSS/JS references
  - [x] 1.3: Set up Bootstrap auto dark/light mode support
  - [x] 1.4: Create custom CSS file for brand-specific styling overrides
  - [x] 1.5: Verify responsive grid system working across breakpoints

- [x] Task 2: Main Navigation Menu (AC: 2, 8)
  - [x] 2.1: Create _Layout.cshtml with Bootstrap navbar component
  - [x] 2.2: Implement navigation menu structure with placeholder links:
    - Dashboard (/)
    - Integrations (/Integrations)
    - Fragments (/Fragments) - placeholder for Epic 2
    - Articles (/Articles) - placeholder for Epic 4
    - Analytics (/Analytics) - placeholder for Epic 5
  - [x] 2.3: Add authentication-aware navigation (login/logout links)
  - [x] 2.4: Implement role-based menu item visibility (Admin vs User)
  - [x] 2.5: Add active page highlighting in navigation
  - [x] 2.6: Ensure mobile-responsive hamburger menu for small screens

- [x] Task 3: Dashboard Page (AC: 3)
  - [x] 3.1: Create HomeController with Index action
  - [x] 3.2: Create Dashboard view (Views/Home/Index.cshtml)
  - [x] 3.3: Display system status indicators (database connection, AWS services)
  - [x] 3.4: Show recent activity overview (placeholder for future epics)
  - [x] 3.5: Add welcome message with user's name
  - [x] 3.6: Create dashboard cards using Bootstrap card components
  - [x] 3.7: Implement responsive grid layout for dashboard widgets

- [ ] Task 4: User Profile Management (AC: 4)
  - [ ] 4.1: Create ProfileController with Edit action
  - [ ] 4.2: Create profile edit view with form validation
  - [ ] 4.3: Implement profile update functionality (name, email)
  - [ ] 4.4: Add password change functionality
  - [ ] 4.5: Display user role and organization information
  - [ ] 4.6: Add success/error messages using Bootstrap alerts

- [ ] Task 5: Error Handling and Error Pages (AC: 5)
  - [ ] 5.1: Create custom error pages (404, 500, 403)
  - [ ] 5.2: Configure error handling middleware in Program.cs
  - [ ] 5.3: Implement user-friendly error messages with recovery guidance
  - [ ] 5.4: Add error logging integration with Serilog
  - [ ] 5.5: Create ErrorController with Error action
  - [ ] 5.6: Style error pages consistently with main UI

- [ ] Task 6: Accessibility Compliance (AC: 6)
  - [ ] 6.1: Add proper ARIA labels to navigation elements
  - [ ] 6.2: Ensure keyboard navigation works for all interactive elements
  - [ ] 6.3: Verify color contrast ratios meet WCAG AA standards
  - [ ] 6.4: Add skip-to-content link for screen readers
  - [ ] 6.5: Test with screen reader (NVDA or JAWS)
  - [ ] 6.6: Add alt text for all images and icons
  - [ ] 6.7: Ensure form labels properly associated with inputs

- [ ] Task 7: Mobile Responsive Design (AC: 7)
  - [ ] 7.1: Test layout on mobile devices (320px, 375px, 414px widths)
  - [ ] 7.2: Test layout on tablets (768px, 1024px widths)
  - [ ] 7.3: Verify touch targets are at least 44x44px
  - [ ] 7.4: Ensure text is readable without zooming
  - [ ] 7.5: Test navigation menu on mobile (hamburger menu)
  - [ ] 7.6: Verify forms are usable on mobile devices

- [ ] Task 8: Shared Layout Components (AC: 1, 2)
  - [ ] 8.1: Create _LoginPartial.cshtml for authentication UI
  - [ ] 8.2: Create _ValidationScriptsPartial.cshtml for client validation
  - [ ] 8.3: Add footer with copyright and links
  - [ ] 8.4: Create breadcrumb navigation component
  - [ ] 8.5: Add loading spinner component for async operations

## Dev Notes

### Architecture Patterns and Constraints

**UI Framework:** Bootstrap 5.3 with auto dark/light mode support, responsive grid system, and accessibility features built-in.

**Server-Side Rendering:** ASP.NET Core MVC with Razor views for optimal performance and SEO. No SPA complexity - all pages server-rendered.

**Navigation Strategy:** Sidebar-first navigation with Bootstrap responsive design and progressive disclosure. Authentication-aware menu items based on user roles.

**Error Handling:** Custom error pages with user-friendly messages and recovery guidance. Error logging integrated with Serilog for monitoring.

**Accessibility:** WCAG AA compliance required for all UI components. Proper ARIA labels, keyboard navigation, color contrast, and screen reader support.

### Project Structure Notes

**Files to Create/Modify:**
```
src/Medley.Web/
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml (main layout with navigation)
│   │   ├── _LoginPartial.cshtml (auth UI)
│   │   ├── _ValidationScriptsPartial.cshtml (validation)
│   │   └── Error.cshtml (error page)
│   ├── Home/
│   │   └── Index.cshtml (dashboard)
│   └── Profile/
│       └── Edit.cshtml (profile management)
├── Controllers/
│   ├── HomeController.cs (dashboard)
│   ├── ProfileController.cs (user profile)
│   └── ErrorController.cs (error handling)
├── wwwroot/
│   ├── css/
│   │   └── site.css (custom styles)
│   ├── js/
│   │   └── site.js (custom scripts)
│   └── lib/ (Bootstrap, jQuery via CDN or local)
└── Program.cs (error handling middleware)
```

**Dependencies:**
- Bootstrap 5.3 (via CDN or NuGet)
- jQuery (for Bootstrap components)
- ASP.NET Core Identity (already configured in Story 1.2)

### References

**Source Documents:**
- [PRD.md](../../PRD.md#user-interface-design-goals) - UI design goals and principles
- [epics.md](../../epics.md#story-18-basic-ui-framework-and-navigation) - Story acceptance criteria
- [solution-architecture.md](../../solution-architecture.md#22-server-side-rendering-strategy) - SSR strategy and component structure
- [tech-spec-epic-1.md](../../tech-spec-epic-1.md#mvc-controllers-and-views-epic-1) - Controller and view specifications

**Key Requirements from PRD:**
- Transparency First: Users can always see source material
- Efficiency for Busy Teams: Streamlined workflows, keyboard shortcuts
- Human-AI Partnership: AI as intelligent research assistant
- Context-Aware Intelligence: Smart suggestions without overwhelming interface

**Architecture Constraints:**
- Server-side rendering for all pages (no SPA)
- Bootstrap 5.3 for responsive design
- WCAG AA accessibility compliance
- Sub-2-second response times for interactive elements

## Dev Agent Record

### Context Reference

- [story-context-1.8.xml](story-context-1.8.xml) - Generated 2025-10-20

### Agent Model Used

<!-- To be filled by DEV agent -->

### Debug Log References

<!-- To be filled by DEV agent -->

### Completion Notes List

<!-- To be filled by DEV agent -->

### File List

<!-- To be filled by DEV agent -->
