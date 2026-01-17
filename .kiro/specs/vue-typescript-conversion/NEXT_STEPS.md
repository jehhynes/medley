# Next Steps for TypeScript Conversion

## Current Status

The TypeScript conversion is **95% complete** but blocked by a critical issue in `Articles.vue`.

### ✅ What's Working
- ✅ C# backend builds successfully
- ✅ NSwag generates TypeScript types automatically
- ✅ All 112 C# unit tests pass
- ✅ TypeScript infrastructure is properly configured
- ✅ Most Vue components successfully converted
- ✅ Composables converted from mixins
- ✅ Utilities converted to TypeScript

### ❌ What's Blocking
- ❌ **Articles.vue has malformed TypeScript conversion**
- ❌ TypeScript compilation fails (86 errors)
- ❌ Vite build fails (parse error)
- ❌ Application cannot run

## Critical Issue: Articles.vue

**File:** `src/Medley.Web/Vue/pages/Articles.vue`  
**Line:** 627  
**Problem:** Incomplete conversion from Options API to Composition API

### The Issue

The file has `<script setup lang="ts">` but line 627 contains:

```typescript
// Methods
const
    async loadArticles() {
      this.ui.loading = true;  // ❌ Wrong!
      // ...
    },
```

**Problems:**
1. `const` keyword without variable name
2. Methods use `this.` (Options API) instead of direct ref access
3. Methods separated by commas (object syntax)
4. Mixes Composition API with Options API patterns

### The Fix

All methods (lines 627-1062) need to be refactored:

**Current (broken):**
```typescript
const
    async loadArticles() {
      this.ui.loading = true;
      this.articles.list = await api.get(...);
    },
```

**Should be:**
```typescript
const loadArticles = async () => {
  ui.value.loading = true;
  articles.value.list = await api.get(...);
};
```

**Key changes needed:**
- Remove the standalone `const` on line 627
- Convert each method to: `const methodName = async () => { ... };`
- Replace ALL `this.` references with direct ref access
- Remove commas between method definitions
- Update `this.router` to `router`
- Update `this.$nextTick()` to `nextTick()`
- Update `this.$refs` to direct ref variables

## Cleanup Tasks

Once Articles.vue is fixed, these files should be deleted:

### Obsolete JavaScript Files
```
src/Medley.Web/Vue/utils/url.js
src/Medley.Web/Vue/utils/helpers.js
src/Medley.Web/Vue/utils/htmlDiff.js
src/Medley.Web/Vue/utils/api.js
src/Medley.Web/Vue/mixins/articleSignalR.js
src/Medley.Web/Vue/mixins/articleVersion.js
src/Medley.Web/Vue/mixins/dropDown.js
src/Medley.Web/Vue/mixins/infiniteScroll.js
src/Medley.Web/Vue/mixins/articleModal.js
src/Medley.Web/Vue/mixins/articleFilter.js
```

These have been superseded by TypeScript versions or converted to composables.

## Recommended Approach

### Option 1: Manual Fix (Recommended)
1. Open `src/Medley.Web/Vue/pages/Articles.vue`
2. Find line 627 (`// Methods` comment)
3. Carefully refactor each method following the pattern above
4. Test incrementally as you convert each method
5. Run `npm run type-check` to verify

### Option 2: Automated Fix
1. Use find/replace with regex to convert method patterns
2. Manually verify each conversion
3. Test thoroughly

### Option 3: Revert and Reconvert
1. If you have a backup of the original JavaScript version
2. Start fresh with proper Composition API conversion
3. Use the other converted components as reference

## Testing After Fix

Once Articles.vue is fixed:

```bash
# 1. Type check
cd src/Medley.Web
npm run type-check

# 2. Build
npm run build

# 3. Run C# tests
cd ../..
dotnet test src/Medley.sln

# 4. Test in development
cd src/Medley.Web
npm run dev
```

## Estimated Time

- **Manual fix:** 2-4 hours (careful refactoring)
- **Automated fix:** 1-2 hours (with testing)
- **Revert and reconvert:** 3-5 hours

## Reference Files

Good examples of properly converted components:
- `src/Medley.Web/Vue/pages/Dashboard.vue`
- `src/Medley.Web/Vue/pages/Fragments.vue`
- `src/Medley.Web/Vue/pages/Sources.vue`
- `src/Medley.Web/Vue/components/ChatPanel.vue`

## Questions?

If you need help with the conversion:
1. Check the reference files above
2. Review the design document: `.kiro/specs/vue-typescript-conversion/design.md`
3. See the validation report: `.kiro/specs/vue-typescript-conversion/VALIDATION_REPORT.md`

---

**Status:** Awaiting Articles.vue fix  
**Last Updated:** January 17, 2026
