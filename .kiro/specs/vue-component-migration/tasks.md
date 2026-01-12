# Implementation Plan: Vue Component Migration

## Overview

This plan outlines the step-by-step conversion of 9 legacy JavaScript Vue components to modern Single File Components (SFCs), updating the Vite build configuration, and updating all references throughout the application.

## Tasks

- [x] 1. Set up project structure and dependencies
  - Create src-js/components directory
  - Add testing dependencies to package.json (vitest, @vue/test-utils, jsdom)
  - Run npm install to install new dependencies
  - _Requirements: 3.1, 3.2_

- [x] 2. Update Vite configuration for multi-component build
  - [x] 2.1 Update vite.config.js to use main.js as entry point
    - Change lib.entry from TiptapEditor.vue to main.js
    - Change lib.fileName to 'components.js'
    - Update output configuration for multiple components
    - Add alias configuration for @ imports
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

  - [x] 2.2 Create main.js entry point
    - Create src-js/main.js file
    - Add placeholder imports for all components (will be filled as components are converted)
    - Add global component registration logic
    - Export all components for individual imports
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 3. Convert VirtualScroller component
  - [x] 3.1 Convert virtual-scroller.js to VirtualScroller.vue
    - Extract template string to `<template>` section
    - Move JavaScript logic to `<script>` section with ES6 export
    - Preserve all props, data, computed, and methods
    - Add component to main.js imports
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [ ]* 3.2 Write unit tests for VirtualScroller
    - Test component renders correctly
    - Test virtual scrolling behavior
    - Test prop handling
    - _Requirements: 7.1, 7.2, 7.3_

- [x] 4. Convert FragmentModal component
  - [x] 4.1 Convert fragment-modal.js to FragmentModal.vue
    - Extract template string to `<template>` section
    - Move JavaScript logic to `<script>` section with ES6 export
    - Preserve all props, data, computed, and methods
    - Add component to main.js imports
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [ ]* 4.2 Write unit tests for FragmentModal
    - Test modal open/close behavior
    - Test prop handling
    - Test event emissions
    - _Requirements: 7.1, 7.2, 7.3_

- [x] 5. Convert VersionsPanel component
  - [x] 5.1 Convert versions-panel.js to VersionsPanel.vue
    - Extract template string to `<template>` section
    - Move JavaScript logic to `<script>` section with ES6 export
    - Preserve all props, data, computed, and methods
    - Add component to main.js imports
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [ ]* 5.2 Write unit tests for VersionsPanel
    - Test version list rendering
    - Test prop handling
    - Test event emissions
    - _Requirements: 7.1, 7.2, 7.3_

- [x] 6. Convert PlanViewer component
  - [x] 6.1 Convert plan-viewer.js to PlanViewer.vue
    - Extract template string to `<template>` section
    - Move JavaScript logic to `<script>` section with ES6 export
    - Preserve all props, data, computed, and methods
    - Add component to main.js imports
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [ ]* 6.2 Write unit tests for PlanViewer
    - Test plan rendering
    - Test prop handling
    - Test event emissions
    - _Requirements: 7.1, 7.2, 7.3_

- [x] 7. Convert SourceList component
  - [x] 7.1 Convert source-list.js to SourceList.vue
    - Extract template string to `<template>` section
    - Move JavaScript logic to `<script>` section with ES6 export
    - Update VirtualScroller usage if present
    - Preserve all props, data, computed, and methods
    - Add component to main.js imports
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [ ]* 7.2 Write unit tests for SourceList
    - Test source list rendering
    - Test prop handling
    - Test event emissions
    - _Requirements: 7.1, 7.2, 7.3_

- [x] 8. Convert FragmentList component
  - [x] 8.1 Convert fragment-list.js to FragmentList.vue
    - Extract template string to `<template>` section
    - Move JavaScript logic to `<script>` section with ES6 export
    - Update VirtualScroller and FragmentModal usage if present
    - Preserve all props, data, computed, and methods
    - Add component to main.js imports
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [ ]* 8.2 Write unit tests for FragmentList
    - Test fragment list rendering
    - Test prop handling
    - Test event emissions
    - _Requirements: 7.1, 7.2, 7.3_

- [x] 9. Convert ArticleTree component
  - [x] 9.1 Convert article-tree.js to ArticleTree.vue
    - Extract template string to `<template>` section
    - Move JavaScript logic to `<script>` section with ES6 export
    - Preserve all props, data, computed, and methods
    - Add component to main.js imports
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [ ]* 9.2 Write unit tests for ArticleTree
    - Test tree rendering
    - Test prop handling
    - Test event emissions
    - _Requirements: 7.1, 7.2, 7.3_

- [x] 10. Convert ArticleList component
  - [x] 10.1 Convert article-list.js to ArticleList.vue
    - Extract template string to `<template>` section
    - Move JavaScript logic to `<script>` section with ES6 export
    - Update VirtualScroller usage if present
    - Preserve all props, data, computed, and methods
    - Add component to main.js imports
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [ ]* 10.2 Write unit tests for ArticleList
    - Test article list rendering
    - Test prop handling
    - Test event emissions
    - _Requirements: 7.1, 7.2, 7.3_

- [x] 11. Convert ChatPanel component
  - [x] 11.1 Convert chat-panel.js to ChatPanel.vue
    - Extract template string to `<template>` section
    - Move JavaScript logic to `<script>` section with ES6 export
    - Preserve all SignalR event handlers
    - Preserve all props, data, computed, and methods
    - Add component to main.js imports
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [ ]* 11.2 Write unit tests for ChatPanel
    - Test chat message rendering
    - Test prop handling
    - Test event emissions
    - Test SignalR event handling (with mocks)
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [x] 12. Move TiptapEditor to components directory
  - [x] 12.1 Move TiptapEditor.vue to src-js/components/
    - Move src-js/TiptapEditor.vue to src-js/components/TiptapEditor.vue
    - Update import in main.js
    - _Requirements: 2.6_

- [x] 13. Checkpoint - Build and test all components
  - Run npm run build to compile all components
  - Verify components.js is generated in wwwroot/js/dist
  - Check for any build errors or warnings
  - Ensure all tests pass, ask the user if questions arise
  - _Requirements: 3.2, 3.6, 8.3_

- [x] 14. Update Razor view references
  - [x] 14.1 Update Articles/Index.cshtml
    - Replace individual component script tags with single components.js reference
    - Remove script tags for: virtual-scroller.js, article-list.js, chat-panel.js, versions-panel.js, fragment-modal.js
    - Add single script tag: `<script src="~/js/dist/components.js" asp-append-version="true"></script>`
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

  - [x] 14.2 Update other Razor views with component references
    - Search for any other views using legacy component scripts
    - Update script references to use components.js
    - Updated: Sources/Index.cshtml, Fragments/Index.cshtml, Areas/Admin/Views/AiPrompts/Index.cshtml
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

  - [ ] 14.3 Verify component usage in templates
    - Ensure all component usage in templates (kebab-case) still works
    - No template changes should be needed
    - _Requirements: 4.4, 5.5_

- [x] 15. Remove legacy component files
  - [x] 15.1 Delete legacy component files
    - Delete all .js files from wwwroot/js/components/
    - Verify no references to deleted files remain
    - Deleted: article-list.js, article-tree.js, chat-panel.js, fragment-list.js, fragment-modal.js, plan-viewer.js, source-list.js, versions-panel.js, virtual-scroller.js
    - _Requirements: 9.1, 9.5_

  - [x] 15.2 Remove legacy component directory
    - Delete wwwroot/js/components/ directory if empty
    - _Requirements: 9.1_

- [ ] 16. Final integration testing
  - [ ] 16.1 Test all pages with converted components
    - Test Articles/Index page loads correctly
    - Test all components render and function properly
    - Test SignalR integration for ChatPanel
    - Test all user interactions work as expected
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 8.1, 8.2, 8.3, 8.4, 8.5_

  - [ ] 16.2 Verify no console errors
    - Check browser console for errors or warnings
    - Verify all components load successfully
    - _Requirements: 8.4_

  - [ ] 16.3 Test development workflow
    - Run npm run dev and verify watch mode works
    - Make a change to a component and verify hot reload
    - _Requirements: 8.1, 8.2, 8.3_

- [ ] 17. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Components are converted in dependency order to minimize issues
- Checkpoints ensure incremental validation
- All component templates remain unchanged (only format changes from string to SFC)
- Component usage in Razor views remains unchanged (kebab-case still works)
- Single bundle approach simplifies deployment and reduces HTTP requests
