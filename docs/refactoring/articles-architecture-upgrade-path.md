# Articles Architecture Refactoring - Top-Down Incremental Upgrade Path

## Philosophy

**Top-Down Refactoring**: Extract logic from the monolithic Articles.vue and immediately replace the old code with the new extracted code. Each step reduces Articles.vue size and moves responsibility to appropriate layers.

**Not building in parallel** - Each extracted piece immediately replaces existing code in the running application.

## Current State

- `Articles.vue`: 1,270 lines containing everything
- State scattered across 9 reactive objects
- Business logic, API calls, tree operations, and UI all mixed together
- No clear separation of concerns

## Target State

- `Articles.vue`: ~300 lines of orchestration and template
- Services: Stateless business logic
- Pinia Store: Centralized state management
- Composables: Reusable UI logic
- Child Components: Focused, single-purpose components

---

## Phase 1: Extract Services (Steps 1-3)

### Step 1: Replace Custom API Wrapper with NSwag Clients

**Current State**:
- Articles.vue uses `api.get()`, `api.post()`, etc. from `utils/api.ts`
- Custom API client duplicates NSwag functionality

**Goal**: Replace all `api.*` calls with NSwag-generated clients directly.

**Tasks**:
1. In Articles.vue, import NSwag clients: `ArticlesApiClient`, etc.
2. Create client instances at top of setup function
3. Replace each `api.get('/api/articles/tree')` with `articlesClient.getTree()`
4. Replace each `api.post('/api/articles', data)` with `articlesClient.create(data)`
5. Update all other API calls to use NSwag clients
6. Remove import of `utils/api.ts`

**Files Modified**:
- `src/Medley.Web/Vue/pages/Articles.vue`

**Verification**:
- ✅ Application loads and displays articles
- ✅ Can create new article
- ✅ Can edit article metadata
- ✅ Can save article content
- ✅ Can move articles
- ✅ All CRUD operations work
- ✅ No references to old `api` object remain in Articles.vue

**Commit**: `refactor: replace custom API wrapper with NSwag clients in Articles.vue`

**Expected Change**: Articles.vue still 1,270 lines but now uses proper typed clients

---

### Step 2: Extract Article Service from Articles.vue

**Current State**:
- Articles.vue has methods like `loadArticles()`, `createArticle()`, `moveArticle()`
- These methods directly call NSwag clients
- Business logic mixed with API calls

**Goal**: Extract API operations into ArticleService and use it in Articles.vue.

**Tasks**:
1. Create `src/Medley.Web/Vue/services/ArticleService.ts`
2. Define `ArticleFilters` interface
3. Move API call logic from Articles.vue methods into service methods:
   - `loadArticles()` → `articleService.getTree()`
   - `createArticle()` → `articleService.create()`
   - `updateArticle()` → `articleService.updateMetadata()`
   - `saveArticle()` → `articleService.updateContent()`
   - etc.
4. Service wraps NSwag client calls with clean interface
5. In Articles.vue, import `articleService` singleton
6. Replace NSwag client calls with service calls
7. Keep business logic (tree updates, UI state) in component

**Service Methods**:
- `getTree(filters?)` - Fetch article tree
- `getById(id)` - Fetch single article
- `create(request)` - Create article
- `updateMetadata(id, updates)` - Update title/type
- `updateContent(id, content)` - Save content
- `move(id, newParentId)` - Move article
- `delete(id)` - Delete article
- `getTypes()` - Fetch article types

**Files Created**:
- `src/Medley.Web/Vue/services/ArticleService.ts`

**Files Modified**:
- `src/Medley.Web/Vue/pages/Articles.vue`

**Verification**:
- ✅ All functionality works identically
- ✅ Articles load and display
- ✅ Can perform all CRUD operations
- ✅ Service handles all API communication
- ✅ No direct NSwag client usage in Articles.vue

**Commit**: `refactor: extract ArticleService from Articles.vue`

**Expected Change**: Articles.vue now ~1,200 lines (removed some API logic)

---

### Step 3: Extract Tree Operations to ArticleTreeService

**Current State**:
- Articles.vue has `buildArticleIndex()`, `buildParentPathCache()`, `sortArticles()`, etc.
- useArticleTree composable has some tree logic
- Tree manipulation scattered across multiple locations

**Goal**: Extract all pure tree operations into ArticleTreeService and use it.

**Tasks**:
1. Create `src/Medley.Web/Vue/services/ArticleTreeService.ts`
2. Move tree operation methods from Articles.vue and useArticleTree:
   - `buildArticleIndex()` → `treeService.buildIndex()`
   - `buildParentPathCache()` → `treeService.buildParentCache()`
   - `sortArticles()` → `treeService.sortArticles()`
   - `insertArticleIntoTree()` → `treeService.insertIntoTree()`
   - `removeArticleFromTree()` → `treeService.removeFromTree()`
   - `flattenTree()` for list view
3. In Articles.vue, import `articleTreeService` singleton
4. Replace inline tree operations with service calls
5. Update useArticleTree composable to use service

**Files Created**:
- `src/Medley.Web/Vue/services/ArticleTreeService.ts`

**Files Modified**:
- `src/Medley.Web/Vue/pages/Articles.vue`
- `src/Medley.Web/Vue/composables/useArticleTree.ts`

**Verification**:
- ✅ Tree view renders correctly
- ✅ Tree nodes sorted properly (Index first, then alphabetical)
- ✅ Can expand/collapse nodes
- ✅ Creating article inserts at correct position
- ✅ Moving article updates tree correctly
- ✅ Deleting article removes from tree

**Commit**: `refactor: extract ArticleTreeService for tree operations`

**Expected Change**: Articles.vue now ~1,100 lines (removed tree logic)

---

## Phase 2: Migrate to Pinia (Steps 4-6)

### Step 4: Install Pinia and Create Article Store

**Current State**:
- Articles.vue has reactive objects: `articles`, `editor`, `ui`, etc.
- State scattered across component
- No centralized state management

**Goal**: Add Pinia and create store with article state, but don't migrate yet.

**Tasks**:
1. Install Pinia: `npm install pinia`
2. Configure Pinia in `main.ts`: create pinia, register with app
3. Create `src/Medley.Web/Vue/stores/useArticleStore.ts`
4. Define store with all article-related state from Articles.vue:
   - `tree`, `types`, `selectedArticleId`, `index`, `expandedIds`, etc.
5. Add getters for computed values
6. Add actions that call ArticleService and ArticleTreeService
7. Store mirrors functionality but isn't used yet

**Files Created**:
- `src/Medley.Web/Vue/stores/useArticleStore.ts`

**Files Modified**:
- `src/Medley.Web/Vue/main.ts` (add Pinia)

**Verification**:
- ✅ Application runs normally (store not used yet)
- ✅ Vue DevTools shows Pinia tab with "article" store
- ✅ Can manually test store in DevTools:
  - `const store = useArticleStore()`
  - `await store.loadArticles()`
  - `console.log(store.articles)` shows data

**Commit**: `feat: add Pinia and create article store (not integrated yet)`

**Expected Change**: Articles.vue still 1,100 lines (no changes yet)

---

### Step 5: Migrate Article List State to Pinia Store

**Current State**:
- Articles.vue has `articles` reactive object with `list`, `index`, `expandedIds`
- Tree data managed locally in component

**Goal**: Replace local article state with Pinia store.

**Tasks**:
1. In Articles.vue setup, create store instance: `const store = useArticleStore()`
2. Replace `loadArticles()` method: call `store.loadArticles()` instead
3. Replace `loadArticleTypes()`: call `store.loadArticleTypes()`
4. Replace `articles.list` with `store.articles` in template
5. Replace `articles.selectedId` with `store.selectedArticleId`
6. Replace `articles.expandedIds` with `store.expandedIds`
7. Replace `articles.types` with `store.articleTypes`
8. Remove local `articles` reactive object
9. Update tree operations to use store methods
10. Update `selectArticle()` to call `store.selectArticle()`

**Files Modified**:
- `src/Medley.Web/Vue/pages/Articles.vue`

**Verification**:
- ✅ Articles load and display in tree
- ✅ Can select articles
- ✅ Can expand/collapse tree nodes
- ✅ Selected article state persists correctly
- ✅ Vue DevTools shows store updating
- ✅ Tree view fully functional

**Commit**: `refactor: migrate article list state to Pinia store`

**Expected Change**: Articles.vue now ~1,000 lines (removed article state)

---

### Step 6: Migrate CRUD Operations to Use Store Actions

**Current State**:
- Articles.vue has `createArticle()`, `updateArticle()`, `moveArticle()` methods
- These update local state after API calls

**Goal**: Replace with store actions that handle both API and state updates.

**Tasks**:
1. Replace `createArticle()` body: call `store.createArticle(request)`
2. Replace `updateArticle()` body: call `store.updateArticleMetadata(id, updates)`
3. Replace `moveArticle()` confirmation callback: call `store.moveArticle()`
4. Remove manual tree insertion/update code from component
5. Add `moveArticle()` action to store if not present
6. Store actions handle API call + tree update atomically

**Files Modified**:
- `src/Medley.Web/Vue/pages/Articles.vue`
- `src/Medley.Web/Vue/stores/useArticleStore.ts` (add moveArticle action)

**Verification**:
- ✅ Can create new article (appears in tree)
- ✅ Can edit article metadata (title/type updates)
- ✅ Can move article (tree updates correctly)
- ✅ Can delete article (removed from tree)
- ✅ All operations update Vue DevTools store state

**Commit**: `refactor: migrate CRUD operations to store actions`

**Expected Change**: Articles.vue now ~900 lines (removed CRUD logic)

---

## Phase 3: Extract Composables (Steps 7-9)

### Step 7: Extract Modal Management to Composable

**Current State**:
- Articles.vue has `createModal` and `editModal` reactive objects
- Modal show/hide logic in component

**Goal**: Extract modal state management to reusable composable.

**Tasks**:
1. Create `src/Medley.Web/Vue/composables/ui/useModalState.ts`
2. Generic composable for modal management
3. Returns `{ visible, data, open(), close() }`
4. In Articles.vue, replace create/edit modal state with composable
5. Update `showCreateArticleModal()` to use `createModal.open()`
6. Update `closeCreateModal()` to use `createModal.close()`
7. Same for edit modal

**Files Created**:
- `src/Medley.Web/Vue/composables/ui/useModalState.ts`

**Files Modified**:
- `src/Medley.Web/Vue/pages/Articles.vue`

**Verification**:
- ✅ Can open create article modal
- ✅ Can close modal
- ✅ Can open edit article modal
- ✅ Modal data populated correctly
- ✅ Creating article works
- ✅ Editing article works

**Commit**: `refactor: extract modal management to composable`

**Expected Change**: Articles.vue now ~850 lines (simplified modal logic)

---

### Step 8: Extract Tab Management to Composable

**Current State**:
- Articles.vue has `contentTabs` reactive object
- Tab switching logic in component

**Goal**: Extract tab state to composable.

**Tasks**:
1. Create `src/Medley.Web/Vue/composables/ui/useTabState.ts`
2. Manages active tab, tab list, add/remove/switch operations
3. In Articles.vue, replace `contentTabs` with `const tabs = useTabState()`
4. Update `switchContentTab()` to use `tabs.switchTab()`
5. Update `closeContentTab()` to use `tabs.removeTab()`
6. Update `openVersionTab()` and `openPlanTab()` to use `tabs.addTab()`

**Files Created**:
- `src/Medley.Web/Vue/composables/ui/useTabState.ts`

**Files Modified**:
- `src/Medley.Web/Vue/pages/Articles.vue`

**Verification**:
- ✅ Editor tab shows by default
- ✅ Can open version tab from versions panel
- ✅ Can open plan tab
- ✅ Can switch between tabs
- ✅ Can close tabs (except editor)
- ✅ Tab state managed correctly

**Commit**: `refactor: extract tab management to composable`

**Expected Change**: Articles.vue now ~800 lines (simplified tab logic)

---

### Step 9: Extract View Mode Logic to Composable

**Current State**:
- Articles.vue has `viewMode` ref and `setViewMode()` method
- View mode saved to localStorage inline

**Goal**: Extract view mode management to composable.

**Tasks**:
1. Create `src/Medley.Web/Vue/composables/features/useArticleView.ts`
2. Manages `viewMode` ref (tree/list/mywork)
3. Handles localStorage persistence
4. Returns `{ viewMode, setViewMode }`
5. In Articles.vue, replace viewMode logic with composable
6. Remove local viewMode ref and setViewMode method

**Files Created**:
- `src/Medley.Web/Vue/composables/features/useArticleView.ts`

**Files Modified**:
- `src/Medley.Web/Vue/pages/Articles.vue`

**Verification**:
- ✅ Can switch between tree/list/mywork views
- ✅ View mode persists after page reload
- ✅ All three views render correctly
- ✅ Badge count on My Work tab correct

**Commit**: `refactor: extract view mode to composable`

**Expected Change**: Articles.vue now ~780 lines (removed view mode logic)

---

## Phase 4: Extract Child Components (Steps 10-13)

### Step 10: Extract Article Sidebar Component

**Current State**:
- Articles.vue template has inline left sidebar markup (200+ lines)
- Contains tree/list/mywork components and header logic

**Goal**: Extract left sidebar to dedicated component.

**Tasks**:
1. Create `src/Medley.Web/Vue/components/articles/ArticleSidebar.vue`
2. Move left sidebar template from Articles.vue
3. Include view mode buttons, filter button, action menu
4. Contains ArticleTree, ArticleList, MyWorkList components
5. Emits events: `select-article`, `create-article`, `edit-article`, `move-article`, `set-view-mode`
6. In Articles.vue, replace sidebar markup with `<article-sidebar>`
7. Pass necessary props and bind event handlers

**Files Created**:
- `src/Medley.Web/Vue/components/articles/ArticleSidebar.vue`

**Files Modified**:
- `src/Medley.Web/Vue/pages/Articles.vue`

**Verification**:
- ✅ Sidebar renders correctly
- ✅ Can switch views
- ✅ Can filter articles
- ✅ Can select articles from any view
- ✅ Can create/edit articles
- ✅ Action menu works

**Commit**: `refactor: extract article sidebar to component`

**Expected Change**: Articles.vue now ~650 lines (removed sidebar template)

---

### Step 11: Extract Content Area Component

**Current State**:
- Articles.vue template has inline content area with tabs and editor (150+ lines)

**Goal**: Extract content area to dedicated component.

**Tasks**:
1. Create `src/Medley.Web/Vue/components/articles/ArticleContentArea.vue`
2. Move main content template from Articles.vue
3. Includes tabs, TiptapEditor, VersionViewer, PlanViewer
4. Receives props: `article`, `tabs`, `activeTab`
5. Emits events: `switch-tab`, `close-tab`, `save`
6. In Articles.vue, replace content area with `<article-content-area>`

**Files Created**:
- `src/Medley.Web/Vue/components/articles/ArticleContentArea.vue`

**Files Modified**:
- `src/Medley.Web/Vue/pages/Articles.vue`

**Verification**:
- ✅ Editor loads with article content
- ✅ Can edit and save content
- ✅ Tabs appear when version/plan exists
- ✅ Can switch tabs
- ✅ Can close version/plan tabs
- ✅ Auto-save works

**Commit**: `refactor: extract content area to component`

**Expected Change**: Articles.vue now ~550 lines (removed content template)

---

### Step 12: Extract Right Sidebar Component

**Current State**:
- Articles.vue template has inline right sidebar (100+ lines)
- Contains ChatPanel and VersionsPanel

**Goal**: Extract right sidebar to dedicated component.

**Tasks**:
1. Create `src/Medley.Web/Vue/components/articles/ArticleRightSidebar.vue`
2. Move right sidebar template from Articles.vue
3. Includes tab switcher and panels
4. Emits events for plan/version/fragment opening
5. In Articles.vue, replace sidebar with `<article-right-sidebar>`

**Files Created**:
- `src/Medley.Web/Vue/components/articles/ArticleRightSidebar.vue`

**Files Modified**:
- `src/Medley.Web/Vue/pages/Articles.vue`

**Verification**:
- ✅ Right sidebar renders
- ✅ Can switch between Assistant and Versions tabs
- ✅ Chat panel functional
- ✅ Versions panel functional
- ✅ Can open version from panel

**Commit**: `refactor: extract right sidebar to component`

**Expected Change**: Articles.vue now ~450 lines (removed right sidebar)

---

### Step 13: Extract Modals to Components

**Current State**:
- Articles.vue template has inline create/edit/filter modal markup (200 lines)

**Goal**: Extract modals to dedicated components.

**Tasks**:
1. Create `src/Medley.Web/Vue/components/modals/CreateArticleModal.vue`
2. Create `src/Medley.Web/Vue/components/modals/EditArticleModal.vue`
3. Create `src/Medley.Web/Vue/components/modals/FilterArticlesModal.vue`
4. Move modal template and logic from Articles.vue
5. Modals emit events: `create`, `update`, `apply-filters`
6. In Articles.vue, replace inline modals with components
7. Use modal composables for visibility state

**Files Created**:
- `src/Medley.Web/Vue/components/modals/CreateArticleModal.vue`
- `src/Medley.Web/Vue/components/modals/EditArticleModal.vue`
- `src/Medley.Web/Vue/components/modals/FilterArticlesModal.vue`

**Files Modified**:
- `src/Medley.Web/Vue/pages/Articles.vue`

**Verification**:
- ✅ Create modal opens and works
- ✅ Edit modal opens and works
- ✅ Filter modal opens and works
- ✅ All modal operations functional
- ✅ Modals close properly

**Commit**: `refactor: extract modals to separate components`

**Expected Change**: Articles.vue now ~300 lines (removed modal templates)

---

## Phase 5: Cleanup (Step 14-15)

### Step 14: Simplify SignalR Integration

**Current State**:
- useArticleSignalR composable has complex queue processing
- SignalR logic mixed with state updates

**Goal**: Simplify SignalR to update store directly.

**Tasks**:
1. Update SignalR event handlers in Articles.vue
2. Call store actions directly on events:
   - `ArticleCreated` → `store.insertArticle()`
   - `ArticleUpdated` → `store.updateArticleMetadata()`
   - `ArticleDeleted` → `store.removeArticle()`
3. Remove queue processing logic
4. Simplify useArticleSignalR composable

**Files Modified**:
- `src/Medley.Web/Vue/pages/Articles.vue`
- `src/Medley.Web/Vue/composables/useArticleSignalR.ts`

**Verification**:
- ✅ Real-time updates work
- ✅ Creating article in another tab appears
- ✅ Updating article in another tab reflects
- ✅ Deleting article in another tab removes it
- ✅ Store updates in DevTools on events

**Commit**: `refactor: simplify SignalR to update store directly`

**Expected Change**: Articles.vue now ~280 lines

---

### Step 15: Remove Deprecated Code

**Current State**:
- Old composables still exist but partially unused
- Some duplicate logic remains

**Goal**: Final cleanup of unused code and consolidation.

**Tasks**:
1. Remove or update useArticleTree (logic now in service + store)
2. Remove or update useArticleModal (logic now in store + modal components)
3. Remove `utils/api.ts` if no longer used anywhere
4. Update any remaining files using old patterns
5. Final verification of all features

**Files Deleted** (if fully replaced):
- `src/Medley.Web/Vue/utils/api.ts` (maybe - check other pages first)

**Files Modified**:
- `src/Medley.Web/Vue/composables/useArticleTree.ts` (simplify or remove)
- `src/Medley.Web/Vue/composables/useArticleModal.ts` (simplify or remove)
- Any other files with references to old patterns

**Verification** (Full Application Test):
- ✅ Articles page loads
- ✅ Tree/List/MyWork views all work
- ✅ Can select articles
- ✅ Can create articles
- ✅ Can edit articles (metadata and content)
- ✅ Can move articles
- ✅ Can delete articles
- ✅ Filter works
- ✅ Search works
- ✅ Real-time updates work
- ✅ Chat assistant works
- ✅ Version history works
- ✅ Plan generation works
- ✅ No console errors
- ✅ Vue DevTools shows clean state

**Commit**: `refactor: remove deprecated code and finalize cleanup`

**Final Result**: Articles.vue is ~270-300 lines

---

## Final Architecture

**Achieved through incremental refactoring:**

```
Articles.vue (~300 lines)
├─ Orchestration only
├─ Uses Pinia store for state
├─ Uses child components for UI
└─ Uses composables for UI logic

Pinia Store
├─ Centralized state management
├─ Calls services for operations
└─ Acts as in-memory cache

Services
├─ ArticleService (API wrapper)
├─ ArticleTreeService (tree operations)
└─ Stateless, reusable

Child Components
├─ ArticleSidebar
├─ ArticleContentArea
├─ ArticleRightSidebar
└─ Modals (Create/Edit/Filter)

Composables
├─ useModalState (generic)
├─ useTabState (generic)
└─ useArticleView (feature-specific)
```

## Success Criteria

- ✅ Articles.vue reduced from 1,270 to ~300 lines
- ✅ Clear separation of concerns
- ✅ All functionality works identically to before
- ✅ Using NSwag clients instead of custom wrapper
- ✅ Pinia manages all state
- ✅ Services handle business logic
- ✅ Components are focused and reusable
- ✅ No regressions in functionality
- ✅ Better developer experience (DevTools, debugging)
- ✅ Foundation for future features
