# Design Document: Vue Component Migration

## Overview

This design outlines the migration of 9 legacy JavaScript-based Vue components to modern Single File Components (SFCs). The migration involves converting components from string-template format to proper `.vue` files, updating the Vite build configuration to compile all components into a single bundle, and updating all references throughout the ASP.NET Core application.

The current architecture has a single TiptapEditor.vue component compiled by Vite, while 9 other components remain as legacy JavaScript files with string templates. This creates inconsistency and prevents leveraging modern Vue tooling benefits like hot module replacement, proper scoping, and better IDE support.

## Architecture

### Current State
```
src/Medley.Web/
├── src-js/
│   └── TiptapEditor.vue (compiled by Vite)
├── wwwroot/js/
│   ├── components/
│   │   ├── article-list.js (legacy)
│   │   ├── article-tree.js (legacy)
│   │   ├── chat-panel.js (legacy)
│   │   ├── fragment-list.js (legacy)
│   │   ├── fragment-modal.js (legacy)
│   │   ├── plan-viewer.js (legacy)
│   │   ├── source-list.js (legacy)
│   │   ├── versions-panel.js (legacy)
│   │   └── virtual-scroller.js (legacy)
│   └── dist/
│       └── tiptap-editor.js (Vite output)
└── vite.config.js (configured for single component)
```

### Target State
```
src/Medley.Web/
├── src-js/
│   ├── components/
│   │   ├── ArticleList.vue
│   │   ├── ArticleTree.vue
│   │   ├── ChatPanel.vue
│   │   ├── FragmentList.vue
│   │   ├── FragmentModal.vue
│   │   ├── PlanViewer.vue
│   │   ├── SourceList.vue
│   │   ├── TiptapEditor.vue
│   │   ├── VersionsPanel.vue
│   │   └── VirtualScroller.vue
│   └── main.js (entry point for all components)
├── wwwroot/js/
│   └── dist/
│       ├── components.js (Vite output - all components)
│       └── components.js.map (source map)
└── vite.config.js (configured for multi-component bundle)
```

## Components and Interfaces

### Component Conversion Pattern

Each legacy component follows this structure:
```javascript
const ComponentName = {
    name: 'ComponentName',
    props: { /* ... */ },
    data() { return { /* ... */ }; },
    computed: { /* ... */ },
    methods: { /* ... */ },
    template: `<div>...</div>`
};
window.ComponentName = ComponentName;
```

This will be converted to SFC format:
```vue
<template>
  <div>...</div>
</template>

<script>
export default {
  name: 'ComponentName',
  props: { /* ... */ },
  data() { return { /* ... */ }; },
  computed: { /* ... */ },
  methods: { /* ... */ }
};
</script>

<style scoped>
/* Component-specific styles if needed */
</style>
```

### Main Entry Point (main.js)

Create a new entry point that imports and exports all components:

```javascript
// src-js/main.js
import ArticleList from './components/ArticleList.vue';
import ArticleTree from './components/ArticleTree.vue';
import ChatPanel from './components/ChatPanel.vue';
import FragmentList from './components/FragmentList.vue';
import FragmentModal from './components/FragmentModal.vue';
import PlanViewer from './components/PlanViewer.vue';
import SourceList from './components/SourceList.vue';
import TiptapEditor from './components/TiptapEditor.vue';
import VersionsPanel from './components/VersionsPanel.vue';
import VirtualScroller from './components/VirtualScroller.vue';

// Export all components for global registration
export {
  ArticleList,
  ArticleTree,
  ChatPanel,
  FragmentList,
  FragmentModal,
  PlanViewer,
  SourceList,
  TiptapEditor,
  VersionsPanel,
  VirtualScroller
};

// Auto-register all components globally if Vue is available
if (typeof window !== 'undefined' && window.Vue) {
  const components = {
    ArticleList,
    ArticleTree,
    ChatPanel,
    FragmentList,
    FragmentModal,
    PlanViewer,
    SourceList,
    TiptapEditor,
    VersionsPanel,
    VirtualScroller
  };

  // Register each component globally
  Object.entries(components).forEach(([name, component]) => {
    window.Vue.component(name, component);
  });
}
```

### Vite Configuration

Update `vite.config.js` to build all components:

```javascript
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'path';

export default defineConfig({
  plugins: [vue()],
  
  build: {
    // Output to wwwroot/js/dist
    outDir: 'wwwroot/js/dist',
    emptyOutDir: true,
    
    // Library mode for building all components as a bundle
    lib: {
      entry: resolve(__dirname, 'src-js/main.js'),
      name: 'MedleyComponents',
      formats: ['iife'],
      fileName: () => 'components.js'
    },
    
    // Externalize Vue since it's already loaded globally
    rollupOptions: {
      external: ['vue'],
      output: {
        globals: {
          vue: 'Vue'
        },
        // Ensure all exports are available on window
        extend: true
      }
    },
    
    // Generate sourcemaps for debugging
    sourcemap: true,
    
    // Minify in production
    minify: 'terser',
    
    // Optimize chunk size
    chunkSizeWarningLimit: 1000
  },
  
  // Resolve configuration
  resolve: {
    alias: {
      vue: 'vue/dist/vue.esm-bundler.js',
      '@': resolve(__dirname, 'src-js')
    }
  },
  
  // Development server configuration
  server: {
    hmr: {
      protocol: 'ws',
      host: 'localhost'
    }
  }
});
```

## Data Models

### Component Props Interface

Each component maintains its existing prop interface. Example from ChatPanel:

```typescript
interface ChatPanelProps {
  articleId: string | null;
  connection: SignalRConnection | null;
}

interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  text: string;
  userName: string | null;
  createdAt: string;
  toolCalls?: ToolCall[];
  isStreaming?: boolean;
}

interface ToolCall {
  name: string;
  callId: string;
  message: string;
  completed: boolean;
  timestamp: string;
  result?: {
    ids?: string[];
  };
}
```

### Component Registration Model

```typescript
interface ComponentRegistry {
  [componentName: string]: VueComponent;
}

interface VueComponent {
  name: string;
  props?: Record<string, PropDefinition>;
  data?: () => Record<string, any>;
  computed?: Record<string, ComputedFunction>;
  methods?: Record<string, Function>;
  template?: string;
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Template Preservation
*For any* converted component, the rendered DOM output should be identical to the legacy version when given the same props and state.
**Validates: Requirements 2.5, 7.1**

### Property 2: Prop Interface Compatibility
*For any* component and any valid prop value, the converted component should accept and process the prop identically to the legacy version.
**Validates: Requirements 2.5, 7.2**

### Property 3: Event Emission Equivalence
*For any* component event, the converted component should emit events with the same name and payload structure as the legacy version.
**Validates: Requirements 2.5, 7.3**

### Property 4: Method Behavior Preservation
*For any* component method and any valid input, the method should produce the same output and side effects in both legacy and converted versions.
**Validates: Requirements 7.1**

### Property 5: Component Registration Completeness
*For all* converted components, each component should be registered globally and accessible via both PascalCase (in code) and kebab-case (in templates).
**Validates: Requirements 4.3, 4.4**

### Property 6: Build Output Validity
*For any* successful build, the output bundle should contain all component definitions and be loadable without errors.
**Validates: Requirements 3.2, 3.6**

### Property 7: Reference Update Completeness
*For all* script references in Razor views, legacy component script tags should be replaced with the new bundle reference, and no legacy references should remain.
**Validates: Requirements 5.1, 5.3, 5.5**

### Property 8: Mixin Import Correctness
*For any* component using mixins, the mixin functionality should work identically after conversion to ES6 imports.
**Validates: Requirements 6.1, 6.4**

## Error Handling

### Build-Time Errors

**Template Syntax Errors**:
- Vite will catch template syntax errors during compilation
- Display clear error messages with file and line numbers
- Prevent deployment of broken components

**Import Resolution Errors**:
- Vite will fail if component imports cannot be resolved
- Check for missing files or incorrect paths
- Verify mixin files are accessible

**Type Errors** (if using TypeScript):
- Catch prop type mismatches at build time
- Validate component interfaces
- Ensure proper typing for emitted events

### Runtime Errors

**Component Registration Failures**:
```javascript
try {
  window.Vue.component(name, component);
} catch (error) {
  console.error(`Failed to register component ${name}:`, error);
  // Continue registering other components
}
```

**Missing Dependencies**:
```javascript
if (!window.Vue) {
  console.error('Vue is not loaded. Components cannot be registered.');
  return;
}

if (!window.signalR) {
  console.warn('SignalR is not loaded. Real-time features may not work.');
}
```

**Prop Validation Errors**:
```javascript
props: {
  articleId: {
    type: String,
    default: null,
    validator: (value) => {
      if (value && typeof value !== 'string') {
        console.error('articleId must be a string');
        return false;
      }
      return true;
    }
  }
}
```

### Migration Error Handling

**Validation Scripts**:
```javascript
// validate-migration.js
const fs = require('fs');
const path = require('path');

// Check all legacy components are converted
const legacyDir = 'wwwroot/js/components';
const newDir = 'src-js/components';

const legacyFiles = fs.readdirSync(legacyDir);
const newFiles = fs.readdirSync(newDir);

legacyFiles.forEach(file => {
  const componentName = path.basename(file, '.js');
  const expectedVueFile = componentName
    .split('-')
    .map(part => part.charAt(0).toUpperCase() + part.slice(1))
    .join('') + '.vue';
  
  if (!newFiles.includes(expectedVueFile)) {
    console.error(`Missing conversion: ${file} -> ${expectedVueFile}`);
  }
});
```

## Testing Strategy

### Unit Testing Approach

**Component Testing**:
- Test each converted Vue component individually using Vue Test Utils
- Verify props are correctly received and processed
- Test computed properties return expected values
- Validate methods produce correct outputs
- Test event emissions with correct payloads
- Verify slot content renders correctly

**Mixin Testing**:
- Test mixin functionality in isolation
- Verify mixin data and computed properties
- Test mixin methods and lifecycle hooks
- Ensure proper integration with components

**Integration Testing**:
- Test page-level component interactions
- Verify data flow between parent and child components
- Test SignalR integration for real-time features
- Validate API integration and state management

### Property-Based Testing

Since this is primarily a refactoring effort, property-based testing will focus on behavioral equivalence:

**Test Configuration**:
- Use Vitest for unit testing (integrates well with Vite)
- Use Vue Test Utils for component testing
- Use custom comparison utilities for behavioral equivalence testing
- Each test tagged with: **Feature: vue-component-migration, Property {number}: {property_text}**
- Behavioral equivalence tests should use representative sample inputs rather than random generation

**Testing Approach**:
- Create test fixtures with representative component states
- Compare rendered output between legacy and converted components
- Verify prop handling, event emission, and method behavior
- Test with various prop combinations and edge cases

### Example Test Structure

```javascript
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import ChatPanel from '@/components/ChatPanel.vue';

describe('ChatPanel Component', () => {
  // Feature: vue-component-migration, Property 1: Template Preservation
  it('renders identical output to legacy version', () => {
    const props = {
      articleId: 'test-123',
      connection: mockConnection
    };
    
    const wrapper = mount(ChatPanel, { props });
    const legacyOutput = renderLegacyChatPanel(props);
    
    expect(wrapper.html()).toEqual(legacyOutput);
  });

  // Feature: vue-component-migration, Property 2: Prop Interface Compatibility
  it('accepts and processes props identically', () => {
    const testCases = [
      { articleId: null, connection: null },
      { articleId: 'test-123', connection: mockConnection },
      { articleId: '', connection: null }
    ];
    
    testCases.forEach(props => {
      const wrapper = mount(ChatPanel, { props });
      expect(wrapper.props()).toEqual(props);
    });
  });

  // Feature: vue-component-migration, Property 3: Event Emission Equivalence
  it('emits events with correct payload', async () => {
    const wrapper = mount(ChatPanel, {
      props: { articleId: 'test-123', connection: mockConnection }
    });
    
    await wrapper.vm.openPlan('plan-456');
    
    expect(wrapper.emitted('open-plan')).toBeTruthy();
    expect(wrapper.emitted('open-plan')[0]).toEqual(['plan-456']);
  });
});
```

### Testing Checklist

- [ ] All components render without errors
- [ ] Props are correctly typed and validated
- [ ] Computed properties return expected values
- [ ] Methods produce correct outputs
- [ ] Events are emitted with correct payloads
- [ ] Slots render content correctly
- [ ] Mixins integrate properly
- [ ] SignalR events are handled correctly
- [ ] API calls work as expected
- [ ] Styles are properly scoped
- [ ] No console errors or warnings
- [ ] Performance is equivalent or better

## Implementation Notes

### Component Conversion Order

Convert components in dependency order:
1. **VirtualScroller** (no dependencies)
2. **FragmentModal** (no dependencies)
3. **VersionsPanel** (no dependencies)
4. **PlanViewer** (no dependencies)
5. **SourceList** (may use VirtualScroller)
6. **FragmentList** (may use VirtualScroller, FragmentModal)
7. **ArticleTree** (no dependencies)
8. **ArticleList** (may use VirtualScroller)
9. **ChatPanel** (complex, has SignalR integration)

### Mixin Handling

If mixins exist in `wwwroot/js/mixins/`, they should be:
1. Moved to `src-js/mixins/`
2. Converted to ES6 modules
3. Imported using ES6 syntax in components

Example:
```javascript
// Legacy
mixins: [window.FormattingMixin]

// Converted
import FormattingMixin from '@/mixins/FormattingMixin';
export default {
  mixins: [FormattingMixin]
}
```

### Development Workflow

1. **Development Mode**: Run `npm run dev` to start Vite in watch mode
2. **Hot Module Replacement**: Changes to .vue files trigger automatic recompilation
3. **Source Maps**: Enable debugging of original source code in browser DevTools
4. **Build Validation**: Run `npm run build` before committing to ensure production build works

### Razor View Updates

Update script references in Razor views (e.g., `Views/Articles/Index.cshtml`):

**Before**:
```html
<script src="~/js/components/virtual-scroller.js" asp-append-version="true"></script>
<script src="~/js/components/article-list.js" asp-append-version="true"></script>
<script src="~/js/components/chat-panel.js" asp-append-version="true"></script>
<script src="~/js/components/versions-panel.js" asp-append-version="true"></script>
<script src="~/js/components/fragment-modal.js" asp-append-version="true"></script>
```

**After**:
```html
<script src="~/js/dist/components.js" asp-append-version="true"></script>
```

### Package.json Scripts

Ensure package.json has proper scripts:
```json
{
  "scripts": {
    "dev": "vite build --watch --mode development",
    "build": "vite build",
    "preview": "vite preview",
    "test": "vitest",
    "test:ui": "vitest --ui"
  }
}
```

### Required Dependencies

Add testing dependencies to package.json:
```json
{
  "devDependencies": {
    "@vitejs/plugin-vue": "^6.0.3",
    "@vue/test-utils": "^2.4.3",
    "vitest": "^1.1.0",
    "jsdom": "^23.0.1",
    "terser": "^5.44.1",
    "vite": "^7.3.1"
  }
}
```
