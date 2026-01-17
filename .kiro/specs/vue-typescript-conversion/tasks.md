# Implementation Plan: Vue 3 TypeScript Conversion with Backend Typing

## Overview

This implementation plan converts the Medley application to use TypeScript across both frontend and backend. The approach includes setting up NSwag for automatic type generation from C# to TypeScript, creating strongly-typed SignalR hubs in C#, converting the Vue 3 frontend to TypeScript, and ensuring type safety throughout the entire stack.

## Tasks

- [x] 1. Set up TypeScript infrastructure and NSwag
  - [x] 1.1 Install frontend TypeScript dependencies
    - Install typescript, @vue/tsconfig, @types/node, fast-check packages
    - Update package.json scripts to support TypeScript
    - Add type checking script
    - _Requirements: 1.1, 1.5, 14.1_
  
  - [x] 1.2 Create tsconfig.json with strict configuration
    - Create tsconfig.json extending @vue/tsconfig/tsconfig.dom.json
    - Enable strict mode, noUnusedLocals, noUnusedParameters
    - Configure path aliases (@/* mapping)
    - Set up include/exclude patterns for Vue directory
    - _Requirements: 1.1, 1.4_
  
  - [x] 1.3 Update Vite configuration for TypeScript
    - Rename vite.config.js to vite.config.ts
    - Add TypeScript type checking configuration
    - Update entry point reference to main.ts
    - Configure esbuild for TypeScript
    - _Requirements: 1.2, 1.6, 10.1, 10.2_
  
  - [x] 1.4 Install and configure NSwag for automatic type generation
    - Add NSwag.AspNetCore NuGet package to Medley.Web project
    - Add NSwag.MSBuild NuGet package for build-time generation
    - Create nswag.json configuration file in Medley.Web root
    - Configure OpenAPI document generation from API controllers
    - Configure TypeScript client generation to Vue/types/generated/api-client.ts
    - Add NSwag generation to MSBuild process
    - _Requirements: 16.1, 16.2, 16.5, 16.7_

- [x] 2. Create C# strongly-typed SignalR hubs
  - [x] 2.1 Create ArticleHub client interface and payload types
    - Create Hubs/Clients/IArticleClient.cs interface in Medley.Application
    - Define all server-to-client method signatures (ArticleCreated, ArticleUpdated, etc.)
    - Create payload record types (ArticleCreatedPayload, ArticleUpdatedPayload, ArticleDeletedPayload, etc.)
    - Document all methods and payloads with XML comments
    - _Requirements: 15.1, 15.7_
  
  - [x] 2.2 Update ArticleHub to use strongly-typed client interface
    - Change ArticleHub to inherit from Hub<IArticleClient>
    - Update all Clients.All.SendAsync calls to use typed methods
    - Update all Clients.Group().SendAsync calls to use typed methods
    - Remove string-based method names ("ArticleCreated", etc.)
    - _Requirements: 15.2, 15.6_
  
  - [x] 2.3 Update services to use strongly-typed IHubContext for ArticleHub
    - Find all IHubContext<ArticleHub> injections in services and background jobs
    - Change to IHubContext<ArticleHub, IArticleClient>
    - Update all SendAsync calls to use typed methods
    - Verify compile-time type checking works
    - _Requirements: 15.5, 15.6, 15.7_
  
  - [x] 2.4 Create AdminHub client interface and update hub
    - Create Hubs/Clients/IAdminClient.cs interface
    - Define AdminNotificationPayload record type
    - Update AdminHub to inherit from Hub<IAdminClient>
    - Update all client method calls to use typed interface
    - Update services using IHubContext<AdminHub> to use IHubContext<AdminHub, IAdminClient>
    - _Requirements: 15.3, 15.4, 15.5, 15.6_

- [-] 3. Create and annotate C# DTOs for API controllers
  - [ ] 3.1 Create DTO classes in Medley.Application/Models/DTOs/
    - Create ArticleDto, ArticleCreateRequest, ArticleUpdateRequest
    - Create FragmentDto, FragmentSearchResult
    - Create UserDto, UserSummaryDto
    - Create ConversationDto, ChatMessageDto, ToolCallDto
    - Create ArticleVersionDto, ArticleVersionComparisonDto
    - Create PlanDto with PlanStatus enum
    - Create SourceDto with SourceType enum
    - Add data validation attributes ([Required], [StringLength], [Range], etc.)
    - Add XML documentation comments for OpenAPI generation
    - _Requirements: 17.1, 17.2, 17.5, 17.6_
  
  - [ ] 3.2 Update API controllers to use strongly-typed DTOs
    - Update ArticlesApiController to use ArticleDto types
    - Update FragmentsApiController to use FragmentDto types
    - Update SourcesApiController to use SourceDto types
    - Add [ProducesResponseType] attributes to all endpoints
    - Use ActionResult<T> return types for typed responses
    - Add XML documentation comments to all endpoints
    - _Requirements: 17.1, 17.2, 17.3, 17.4, 17.6_
  
  - [ ] 3.3 Run NSwag generation and verify TypeScript output
    - Build the Medley.Web project to trigger NSwag generation
    - Verify Vue/types/generated/api-client.ts is created
    - Verify all DTO interfaces are generated correctly
    - Verify MedleyApiClient class is generated with typed methods
    - Check that TypeScript types match C# DTOs
    - _Requirements: 16.3, 16.4, 16.6_

- [ ] 4. Create frontend type structure with generated types
  - [ ] 4.1 Create types directory structure
    - Create Vue/types/generated/ directory (for NSwag output)
    - Create Vue/types/signalr/ directory
    - Create Vue/types/dtos/extensions.ts for manual type extensions
    - Create Vue/types/index.ts for central exports
    - Update .gitignore to track generated files
    - _Requirements: 2.9, 2.10, 16.7, 16.8_
  
  - [ ] 4.2 Create manual type extensions for SignalR-specific types
    - Create types/dtos/extensions.ts
    - Re-export generated types from api-client.ts
    - Add ConversationSummary interface (not in REST API)
    - Add StreamUpdateType enum
    - Add ChatStreamUpdate interface
    - Export all extended types
    - _Requirements: 2.5_

- [ ] 5. Create SignalR hub type definitions
  - [ ] 5.1 Create ArticleHub TypeScript types
    - Create types/signalr/article-hub.ts
    - Define all ArticleHub payload interfaces (ArticleCreatedPayload, ArticleUpdatedPayload, etc.)
    - Define ArticleHubServerMethods interface
    - Define ArticleHubConnection type with typed on() method
    - _Requirements: 3.1, 3.3, 3.6_
  
  - [ ] 5.2 Create AdminHub TypeScript types and exports
    - Create types/signalr/admin-hub.ts
    - Define AdminNotificationPayload interface
    - Define AdminHubServerMethods interface
    - Define AdminHubConnection type
    - Create types/signalr/index.ts for exports
    - _Requirements: 3.2, 3.3, 3.7_

- [ ] 6. Create typed API client wrapper and utilities
  - [ ] 6.1 Create API client wrapper using generated NSwag client
    - Create utils/api.ts
    - Import MedleyApiClient and ApiException from generated types
    - Create ApiClientWrapper class that wraps generated client
    - Organize methods by resource (articles, fragments, sources, etc.)
    - Export api singleton instance and ApiException
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 16.4_
  
  - [ ]* 6.2 Write property tests for API client wrapper
    - **Property 1: HTTP Method Type Safety**
    - **Validates: Requirements 4.1, 4.2, 4.3, 4.4**
    - Test that API methods return correctly typed responses
    - Use fast-check to generate random valid request/response data
    - Mock fetch responses
    - Run 100 iterations
  
  - [ ]* 6.3 Write property test for API error handling
    - **Property 2: API Error Typing**
    - **Validates: Requirements 4.6**
    - Test that failed requests throw ApiException with correct properties
    - Generate random HTTP error status codes (400-599)
    - Verify error type, status, and message
    - Run 100 iterations
  
  - [ ]* 6.4 Write unit tests for API client edge cases
    - Test 204 No Content response handling
    - Test network failure scenarios
    - Test JSON parsing errors
    - _Requirements: 4.5_
  
  - [ ] 6.5 Create typed SignalR connection utilities
    - Create utils/signalr.ts
    - Import SignalR and hub type definitions
    - Implement createArticleHubConnection function returning ArticleHubConnection
    - Implement createAdminHubConnection function returning AdminHubConnection
    - Add reconnection event handlers
    - _Requirements: 3.4, 3.5_
  
  - [ ] 6.6 Convert remaining utility files to TypeScript
    - Convert utils/helpers.js to helpers.ts with typed function signatures
    - Convert utils/htmlDiff.js to htmlDiff.ts with typed parameters
    - Convert utils/url.js to url.ts with typed parameters
    - Add JSDoc comments for complex functions
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

- [ ] 7. Create type guards and validation utilities
  - [ ] 7.1 Create type guard functions for critical DTOs
    - Create types/guards.ts
    - Implement isArticle type guard
    - Implement isUserSummary type guard
    - Implement isFragment type guard
    - Implement isChatMessage type guard
    - Export all type guards
    - _Requirements: 11.5_
  
  - [ ]* 7.2 Write property tests for type guards
    - **Property 3: Runtime Validation Correctness**
    - **Validates: Requirements 11.1, 11.2, 11.5**
    - Test type guards with randomly generated valid data (should return true)
    - Test type guards with randomly generated invalid data (should return false)
    - Use fast-check to generate test data
    - Run 100 iterations per type guard
  
  - [ ]* 7.3 Write unit tests for type guard edge cases
    - Test null and undefined inputs
    - Test partial objects
    - Test objects with extra properties
    - Test nested object validation
    - _Requirements: 11.5_
  
  - [ ] 7.4 Create validation error handling utilities
    - Create types/utils.ts with utility types
    - Add validation error logging function
    - Add graceful error handler wrapper
    - _Requirements: 11.3, 11.4_
  
  - [ ]* 7.5 Write property test for graceful error handling
    - **Property 4: Graceful Validation Failure Handling**
    - **Validates: Requirements 11.4**
    - Test that validation failures don't crash the application
    - Generate random invalid payloads
    - Verify error is logged and handled gracefully
    - Run 100 iterations

- [ ] 8. Checkpoint - Verify backend and infrastructure
  - Ensure C# project builds successfully with strongly-typed hubs
  - Verify NSwag generates TypeScript types correctly
  - Ensure TypeScript compilation succeeds with generated types
  - Check that type definitions are properly exported
  - Ask the user if questions arise

- [ ] 9. Convert composables to TypeScript
  - [ ] 9.1 Convert existing composables to TypeScript
    - Rename useArticleTree.js to useArticleTree.ts
    - Add TypeScript types for parameters and return values
    - Rename useArticleVersions.js to useArticleVersions.ts and add types
    - Rename useArticleView.js to useArticleView.ts and add types
    - Rename useMyWork.js to useMyWork.ts and add types
    - Rename useSidebarState.js to useSidebarState.ts and add types
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_
  
  - [ ] 9.2 Convert articleSignalR mixin to typed composable
    - Create composables/useArticleSignalR.ts
    - Define UseArticleSignalROptions interface
    - Define SignalRQueueUpdate interface
    - Implement useArticleSignalR composable with typed event handlers
    - Use typed ArticleHubConnection
    - Add proper TypeScript types for all state and functions
    - _Requirements: 7.1_
  
  - [ ] 9.3 Convert articleFilter mixin to typed composable
    - Create composables/useArticleFilter.ts
    - Define filter options interface
    - Implement useArticleFilter with typed parameters and return value
    - _Requirements: 7.2_
  
  - [ ] 9.4 Convert articleModal mixin to typed composable
    - Create composables/useArticleModal.ts
    - Define modal state interface
    - Implement useArticleModal with typed state and methods
    - _Requirements: 7.3_
  
  - [ ] 9.5 Convert remaining mixins to typed composables
    - Convert articleVersion mixin to composables/useArticleVersion.ts
    - Convert dropDown mixin to composables/useDropDown.ts
    - Convert infiniteScroll mixin to composables/useInfiniteScroll.ts
    - Add TypeScript types for all parameters and return values
    - _Requirements: 7.4, 7.5, 7.6_

- [ ] 10. Convert core components to TypeScript
  - [ ] 10.1 Convert ArticleList component
    - Update ArticleList.vue to use `<script setup lang="ts">`
    - Define Props interface for component props
    - Define Emits interface for component events
    - Type all reactive state with ref<T> and reactive<T>
    - Type all computed properties
    - Use typed API client for data fetching
    - Update component to use useArticleSignalR composable
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 7.7_
  
  - [ ] 10.2 Convert ArticleTree component
    - Update ArticleTree.vue to use `<script setup lang="ts">`
    - Define Props and Emits interfaces
    - Type all state and computed properties
    - Use generated ArticleDto and related types
    - Use typed API client
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_
  
  - [ ] 10.3 Convert ChatPanel component
    - Update ChatPanel.vue to use `<script setup lang="ts">`
    - Define Props and Emits interfaces
    - Type all state with ChatMessage, Conversation types from generated DTOs
    - Use typed SignalR connection for chat events
    - Use typed API client for message sending
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_

- [ ] 11. Convert supporting components to TypeScript
  - [ ] 11.1 Convert FragmentList and FragmentModal components
    - Update FragmentList.vue to use `<script setup lang="ts">`
    - Update FragmentModal.vue to use `<script setup lang="ts">`
    - Define Props and Emits interfaces for both
    - Use generated Fragment DTO types
    - Use typed API client
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_
  
  - [ ] 11.2 Convert SourceList component
    - Update SourceList.vue to use `<script setup lang="ts">`
    - Define Props and Emits interfaces
    - Use generated Source DTO types
    - Use typed API client
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_
  
  - [ ] 11.3 Convert VersionsPanel and VersionViewer components
    - Update VersionsPanel.vue to use `<script setup lang="ts">`
    - Update VersionViewer.vue to use `<script setup lang="ts">`
    - Define Props and Emits interfaces for both
    - Use generated ArticleVersion DTO types
    - Use typed API client
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_
  
  - [ ] 11.4 Convert PlanViewer component
    - Update PlanViewer.vue to use `<script setup lang="ts">`
    - Define Props and Emits interfaces
    - Use generated Plan DTO types
    - Use typed API client
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_
  
  - [ ] 11.5 Convert remaining components
    - Update TiptapEditor.vue to use `<script setup lang="ts">`
    - Update VirtualScroller.vue to use `<script setup lang="ts">`
    - Update VerticalMenu.vue to use `<script setup lang="ts">`
    - Update Backdrop.vue to use `<script setup lang="ts">`
    - Update MobileHeader.vue to use `<script setup lang="ts">`
    - Update MyWorkList.vue to use `<script setup lang="ts">`
    - Update ToolCallItem.vue to use `<script setup lang="ts">`
    - Define Props and Emits interfaces for all
    - Type all state and computed properties
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_

- [ ] 12. Convert page components to TypeScript
  - [ ] 12.1 Convert Articles page
    - Update pages/Articles.vue to use `<script setup lang="ts">`
    - Define Props interface if needed
    - Type all state and computed properties
    - Use typed composables and API client
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_
  
  - [ ] 12.2 Convert remaining pages
    - Update pages/Dashboard.vue to use `<script setup lang="ts">`
    - Update pages/Fragments.vue to use `<script setup lang="ts">`
    - Update pages/Sources.vue to use `<script setup lang="ts">`
    - Update pages/AiPrompts.vue to use `<script setup lang="ts">`
    - Define Props interfaces where needed
    - Type all state and computed properties
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_

- [ ] 13. Convert router and main entry point
  - [ ] 13.1 Convert router configuration to TypeScript
    - Rename router/index.js to router/index.ts
    - Import RouteRecordRaw type from vue-router
    - Type all route definitions
    - Type route meta fields with custom interface
    - Type navigation guards with proper parameter types
    - _Requirements: 9.1, 9.2, 9.3, 9.4_
  
  - [ ] 13.2 Convert main entry point and App component
    - Rename main.js to main.ts
    - Add proper TypeScript types for app initialization
    - Update App.vue to use `<script setup lang="ts">`
    - Type any app-level state
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_

- [ ] 14. Final checkpoint and validation
  - Ensure C# project builds successfully
  - Ensure NSwag generates types on every build
  - Ensure all TypeScript compilation succeeds with no errors
  - Run all unit tests and property-based tests
  - Verify application builds successfully
  - Test application functionality in development mode
  - Check for any remaining .js files that should be converted
  - Verify type coverage is comprehensive
  - Ask the user if questions arise

- [ ] 15. Clean up and documentation
  - [ ] 15.1 Remove old JavaScript files and mixins
    - Delete mixins directory (all converted to composables)
    - Verify no .js files remain in Vue directory (except config files)
    - Update any remaining imports
    - _Requirements: 7.7_
  
  - [ ] 15.2 Add JSDoc comments to complex types
    - Add documentation to DTO interfaces
    - Add documentation to composable functions
    - Add documentation to utility functions
    - Document type guard usage
    - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6_
  
  - [ ] 15.3 Update build scripts and verify production build
    - Verify production build succeeds
    - Check bundle size hasn't increased significantly
    - Test production build functionality
    - Update any build documentation
    - _Requirements: 10.3, 10.4, 10.5, 10.6_

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties
- Unit tests validate specific examples and edge cases
- The conversion is incremental - the application remains functional at each step
- TypeScript strict mode catches errors at compile time
- Type guards provide runtime validation for external data
- NSwag automatically generates TypeScript types from C# DTOs, keeping frontend and backend in sync
- Strongly-typed SignalR hubs provide compile-time safety for real-time messaging
