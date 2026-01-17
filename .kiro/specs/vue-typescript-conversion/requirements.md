# Requirements Document: Vue 3 TypeScript Conversion

## Introduction

This specification defines the requirements for converting the Medley Vue 3 application from JavaScript to TypeScript. The conversion will introduce strong typing throughout the application, including DTOs that mirror C# backend models, typed SignalR hub connections, and a typed API client layer. The goal is to improve type safety, developer experience, and maintainability while preserving all existing functionality.

## Glossary

- **DTO**: Data Transfer Object - structures used to transfer data between frontend and backend
- **SignalR**: Real-time communication library for bidirectional client-server messaging
- **Hub**: SignalR server endpoint that clients connect to for real-time updates
- **API_Client**: TypeScript module providing typed HTTP request methods
- **Type_Guard**: TypeScript function that validates runtime data matches expected types
- **Composition_API**: Vue 3's modern API for component logic using setup functions
- **Vite**: Build tool and development server for the Vue application
- **ArticleHub**: SignalR hub for article-related real-time updates
- **AdminHub**: SignalR hub for administrative notifications
- **Component**: Vue single-file component (.vue file)
- **Composable**: Reusable composition function for shared logic
- **Mixin**: Legacy Vue pattern for sharing component logic (to be converted)

## Requirements

### Requirement 1: TypeScript Infrastructure Setup

**User Story:** As a developer, I want TypeScript properly configured in the project, so that I can write type-safe code with proper tooling support.

#### Acceptance Criteria

1. THE System SHALL include a tsconfig.json file with strict type checking enabled
2. THE System SHALL configure Vite to process TypeScript files
3. THE System SHALL include @vue/tsconfig as the base TypeScript configuration
4. THE System SHALL configure path aliases to match existing JavaScript configuration
5. THE System SHALL include TypeScript type definitions for all third-party libraries
6. THE System SHALL configure the build process to type-check before compilation
7. THE System SHALL support .ts and .vue files with TypeScript script blocks

### Requirement 2: DTO Type Definitions

**User Story:** As a developer, I want TypeScript interfaces for all data structures, so that API responses and SignalR messages are strongly typed.

#### Acceptance Criteria

1. THE System SHALL define TypeScript interfaces for all article-related DTOs
2. THE System SHALL define TypeScript interfaces for all fragment-related DTOs
3. THE System SHALL define TypeScript interfaces for all source-related DTOs
4. THE System SHALL define TypeScript interfaces for all user-related DTOs
5. THE System SHALL define TypeScript interfaces for all chat/conversation DTOs
6. THE System SHALL define TypeScript interfaces for all version-related DTOs
7. THE System SHALL define TypeScript interfaces for all plan-related DTOs
8. THE System SHALL define TypeScript interfaces for all template-related DTOs
9. THE System SHALL organize DTO types in a dedicated types directory
10. THE System SHALL export all DTO types from a central index file

### Requirement 3: SignalR Hub Type Definitions

**User Story:** As a developer, I want typed SignalR hub connections, so that real-time message handlers have compile-time type safety.

#### Acceptance Criteria

1. THE System SHALL define TypeScript interfaces for ArticleHub server-to-client methods
2. THE System SHALL define TypeScript interfaces for AdminHub server-to-client methods
3. THE System SHALL define TypeScript types for all SignalR event payloads
4. THE System SHALL provide a typed connection factory function for ArticleHub
5. THE System SHALL provide a typed connection factory function for AdminHub
6. THE System SHALL type the HubConnection.on() method parameters
7. THE System SHALL include TypeScript definitions for SignalR reconnection events

### Requirement 4: Typed API Client

**User Story:** As a developer, I want a typed API client, so that HTTP requests and responses are type-safe throughout the application.

#### Acceptance Criteria

1. THE API_Client SHALL provide a typed get() method with generic return type parameter
2. THE API_Client SHALL provide a typed post() method with generic request and response type parameters
3. THE API_Client SHALL provide a typed put() method with generic request and response type parameters
4. THE API_Client SHALL provide a typed delete() method with appropriate return type
5. THE API_Client SHALL handle 204 No Content responses correctly with null return type
6. THE API_Client SHALL throw typed errors for failed requests
7. THE API_Client SHALL validate response content types match expected types
8. THE API_Client SHALL include proper TypeScript generics for type inference

### Requirement 5: Component TypeScript Conversion

**User Story:** As a developer, I want all Vue components converted to TypeScript, so that component props, emits, and internal logic are type-safe.

#### Acceptance Criteria

1. WHEN a component is converted, THE System SHALL use `<script setup lang="ts">` syntax
2. WHEN a component has props, THE System SHALL define props using TypeScript interfaces
3. WHEN a component emits events, THE System SHALL define emits with typed payloads
4. WHEN a component uses refs, THE System SHALL type all ref declarations
5. WHEN a component uses reactive state, THE System SHALL type all reactive declarations
6. WHEN a component uses computed properties, THE System SHALL infer or declare return types
7. WHEN a component calls APIs, THE System SHALL use typed API client methods
8. WHEN a component uses SignalR, THE System SHALL use typed hub connections

### Requirement 6: Composable TypeScript Conversion

**User Story:** As a developer, I want all composables converted to TypeScript, so that shared logic has proper type definitions and return types.

#### Acceptance Criteria

1. THE System SHALL convert all composable files from .js to .ts
2. THE System SHALL define TypeScript interfaces for composable return types
3. THE System SHALL type all composable parameters
4. THE System SHALL type all internal state within composables
5. THE System SHALL export typed functions from composables

### Requirement 7: Mixin Elimination and Conversion

**User Story:** As a developer, I want mixins converted to composables, so that the codebase uses modern Vue 3 patterns with better type inference.

#### Acceptance Criteria

1. THE System SHALL convert articleSignalR mixin to a typed composable
2. THE System SHALL convert articleFilter mixin to a typed composable
3. THE System SHALL convert articleModal mixin to a typed composable
4. THE System SHALL convert articleVersion mixin to a typed composable
5. THE System SHALL convert dropDown mixin to a typed composable
6. THE System SHALL convert infiniteScroll mixin to a typed composable
7. WHEN a mixin is converted, THE System SHALL update all components using that mixin

### Requirement 8: Utility Function TypeScript Conversion

**User Story:** As a developer, I want all utility functions converted to TypeScript, so that helper functions have proper type signatures.

#### Acceptance Criteria

1. THE System SHALL convert all utility files from .js to .ts
2. THE System SHALL type all utility function parameters
3. THE System SHALL type all utility function return values
4. THE System SHALL type all internal variables within utility functions

### Requirement 9: Router TypeScript Conversion

**User Story:** As a developer, I want the Vue Router configuration converted to TypeScript, so that route definitions and navigation are type-safe.

#### Acceptance Criteria

1. THE System SHALL convert router/index.js to router/index.ts
2. THE System SHALL type all route definitions
3. THE System SHALL type route meta fields
4. THE System SHALL type navigation guards with proper parameter types

### Requirement 10: Build Configuration Updates

**User Story:** As a developer, I want the build configuration updated for TypeScript, so that the application builds correctly with type checking.

#### Acceptance Criteria

1. THE System SHALL update vite.config.js to vite.config.ts
2. THE System SHALL configure Vite to perform TypeScript type checking
3. THE System SHALL update package.json scripts for TypeScript
4. THE System SHALL include TypeScript in the development build process
5. THE System SHALL include TypeScript in the production build process
6. THE System SHALL configure source maps for TypeScript debugging

### Requirement 11: Type Safety and Validation

**User Story:** As a developer, I want runtime type validation for external data, so that API responses and SignalR messages are validated at runtime.

#### Acceptance Criteria

1. WHEN receiving API responses, THE System SHALL validate response structure matches expected DTO types
2. WHEN receiving SignalR messages, THE System SHALL validate message structure matches expected types
3. IF validation fails, THEN THE System SHALL log a type mismatch error
4. IF validation fails, THEN THE System SHALL handle the error gracefully without crashing
5. THE System SHALL provide Type_Guard functions for critical DTO types

### Requirement 12: Incremental Migration Support

**User Story:** As a developer, I want to migrate incrementally, so that the application remains functional during the conversion process.

#### Acceptance Criteria

1. THE System SHALL support mixed .js and .ts files during migration
2. THE System SHALL allow TypeScript files to import JavaScript files
3. THE System SHALL maintain backward compatibility with existing JavaScript code
4. THE System SHALL not break existing functionality when converting individual files
5. THE System SHALL allow the application to run with partially converted codebase

### Requirement 13: Developer Experience Improvements

**User Story:** As a developer, I want improved IDE support, so that I have better autocomplete, type checking, and refactoring capabilities.

#### Acceptance Criteria

1. THE System SHALL provide IntelliSense for all DTO properties
2. THE System SHALL provide IntelliSense for all API client methods
3. THE System SHALL provide IntelliSense for all SignalR hub methods
4. THE System SHALL show type errors in the IDE before compilation
5. THE System SHALL support go-to-definition for all typed entities
6. THE System SHALL support find-all-references for typed entities

### Requirement 14: Testing Infrastructure Updates

**User Story:** As a developer, I want testing infrastructure updated for TypeScript, so that tests can be written in TypeScript with proper typing.

#### Acceptance Criteria

1. THE System SHALL configure Vitest to support TypeScript test files
2. THE System SHALL allow .test.ts and .spec.ts test files
3. THE System SHALL provide typed test utilities and helpers
4. THE System SHALL type all test fixtures and mock data
5. THE System SHALL maintain existing test functionality during conversion

### Requirement 15: C# Strongly-Typed SignalR Hubs

**User Story:** As a backend developer, I want strongly-typed SignalR hubs in C#, so that server-to-client method calls are type-safe and refactor-friendly.

#### Acceptance Criteria

1. THE ArticleHub SHALL define an interface for server-to-client methods
2. THE ArticleHub SHALL inherit from Hub<TClient> with the client interface
3. THE AdminHub SHALL define an interface for server-to-client methods
4. THE AdminHub SHALL inherit from Hub<TClient> with the client interface
5. WHEN injecting IHubContext, THE System SHALL use IHubContext<THub, TClient> for type safety
6. WHEN calling client methods, THE System SHALL use strongly-typed method calls instead of string-based SendAsync
7. THE System SHALL provide compile-time checking for client method names and parameters

### Requirement 16: Automatic TypeScript Type Generation

**User Story:** As a developer, I want TypeScript types automatically generated from C# DTOs, so that frontend and backend types stay synchronized without manual maintenance.

#### Acceptance Criteria

1. THE System SHALL integrate NSwag for OpenAPI specification generation
2. THE System SHALL configure NSwag to generate TypeScript client from OpenAPI spec
3. THE System SHALL generate TypeScript interfaces for all API request/response DTOs
4. THE System SHALL generate TypeScript API client with typed methods
5. THE System SHALL include generated types in the build process
6. THE System SHALL regenerate types when C# DTOs change
7. THE System SHALL place generated TypeScript files in a dedicated directory
8. THE System SHALL configure git to track generated TypeScript files

### Requirement 17: API Controller Type Safety

**User Story:** As a backend developer, I want API controllers to use strongly-typed DTOs, so that request/response types are enforced at compile time.

#### Acceptance Criteria

1. WHEN defining API endpoints, THE System SHALL use typed DTO classes for request bodies
2. WHEN defining API endpoints, THE System SHALL use typed DTO classes for response bodies
3. THE System SHALL annotate API methods with [ProducesResponseType] attributes
4. THE System SHALL use ActionResult<T> return types for typed responses
5. THE System SHALL validate request DTOs using data annotations
6. THE System SHALL document all API endpoints with XML comments for OpenAPI generation
