# Mixin Conversion Plan

## Overview

This document outlines the plan to convert 6 legacy JavaScript mixins from `wwwroot/js/mixins/` to ES6 modules in `src-js/mixins/`, update components to import them, and remove the legacy files.

## Current State

### Legacy Mixins (wwwroot/js/mixins/)
1. **dropdown.js** - Handles closing other dropdowns when one is opened
2. **article-filter.js** - Article filtering functionality
3. **article-modal.js** - Article modal management
4. **article-signalr.js** - SignalR integration for articles
5. **article-version.js** - Article version management
6. **infinite-scroll.js** - Infinite scroll functionality

### Current Usage Pattern
```javascript
// Legacy mixin definition
(function() {
    window.dropdownMixin = {
        methods: {
            handleDropdownClick(event, articleId) {
                // ...
            }
        }
    };
})();

// Legacy usage in components
if (window.dropdownMixin && window.dropdownMixin.methods.handleDropdownClick) {
    window.dropdownMixin.methods.handleDropdownClick.call(this, event, articleId);
}
```

## Target State

### Converted Mixins (src-js/mixins/)
All mixins will be converted to ES6 modules:

```javascript
// src-js/mixins/dropdown.js
export default {
    methods: {
        handleDropdownClick(event, articleId) {
            // ...
        }
    }
};
```

### Modern Usage Pattern
```javascript
// In Vue component
import dropdownMixin from '@/mixins/dropdown';

export default {
    name: 'ArticleList',
    mixins: [dropdownMixin],
    // ...
};
```

## Conversion Steps

### Phase 1: Convert Mixin Files (Tasks 16.1-16.6)
For each mixin:
1. Create new file in `src-js/mixins/` with same base name
2. Remove IIFE wrapper `(function() { ... })()`
3. Remove `window.mixinName =` assignment
4. Add `export default` before mixin object
5. Preserve all methods, data, computed properties, and lifecycle hooks
6. Test that mixin exports correctly

### Phase 2: Update Components (Tasks 17.1-17.3)
1. **ArticleList.vue** - Import and use dropdown mixin
2. **ArticleTree.vue** - Import and use dropdown mixin
3. Search for other components using mixins via window references
4. Update any found components to use ES6 imports
5. Remove window.mixinName fallback code

### Phase 3: Update Razor Views (Tasks 18.1-18.2)
1. Search all `.cshtml` files for mixin script references
2. Document which views reference which mixins
3. Remove all `<script>` tags referencing mixin files
4. Mixins are now bundled in `components.js` automatically

### Phase 4: Cleanup (Tasks 19.1-19.2)
1. Delete all `.js` files from `wwwroot/js/mixins/`
2. Verify no references remain in codebase
3. Delete `wwwroot/js/mixins/` directory

### Phase 5: Testing (Task 20)
1. Run `npm run build` to compile components with mixins
2. Verify `components.js` includes mixin code
3. Test ArticleList and ArticleTree dropdown functionality
4. Test any other components using mixins
5. Verify no console errors

## Known Component Dependencies

Based on code analysis:
- **ArticleList.vue** → uses dropdown mixin
- **ArticleTree.vue** → uses dropdown mixin
- Other mixins may be used in pages or legacy components not yet converted

## Benefits

1. **Modern ES6 modules** - Better tooling support and IDE integration
2. **Bundled with components** - Single HTTP request instead of multiple
3. **Tree-shaking** - Unused mixin code can be eliminated
4. **Type safety** - Easier to add TypeScript types later
5. **Hot module replacement** - Mixin changes trigger automatic reload
6. **Consistent architecture** - All Vue code uses same module system

## Risks & Mitigation

### Risk: Breaking existing functionality
**Mitigation**: 
- Convert one mixin at a time
- Test after each conversion
- Keep legacy files until all conversions complete

### Risk: Missing mixin usage in legacy code
**Mitigation**:
- Search entire codebase for `window.mixinName` references
- Document all usage before conversion
- Test all pages that might use mixins

### Risk: Build configuration issues
**Mitigation**:
- Vite already configured to handle ES6 imports
- Test build after first mixin conversion
- Verify sourcemaps work for debugging

## Success Criteria

- [ ] All 6 mixins converted to ES6 modules
- [ ] All components using mixins updated to import them
- [ ] All Razor view script references removed
- [ ] All legacy mixin files deleted
- [ ] Build succeeds and generates components.js
- [ ] All components using mixins function correctly
- [ ] No console errors in browser
- [ ] Development workflow (watch mode) works with mixins

## Timeline

Estimated time: 2-3 hours
- Mixin conversion: 30 minutes (6 files × 5 minutes each)
- Component updates: 30 minutes
- Razor view updates: 15 minutes
- Testing: 45 minutes
- Cleanup: 15 minutes
- Buffer: 30 minutes

## References

- **Requirements**: `.kiro/specs/vue-component-migration/requirements.md` (Requirement 6)
- **Design**: `.kiro/specs/vue-component-migration/design.md` (Mixin Handling section)
- **Tasks**: `.kiro/specs/vue-component-migration/tasks.md` (Tasks 16-20)
