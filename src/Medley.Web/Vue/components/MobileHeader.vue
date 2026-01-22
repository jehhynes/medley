<template>
  <header class="mobile-header" :class="headerClass">
    <div class="mobile-header-left">
      <button 
        id="mobile-left-sidebar-toggle" 
        class="btn btn-link text-body p-0 d-lg-none"
        @click="toggleLeftSidebar"
      >
        <i class="bi bi-layout-sidebar"></i>
      </button>
    </div>
    <div class="mobile-header-center">
      <span class="mobile-header-title">{{ pageTitle }}</span>
    </div>
    <div class="mobile-header-right">
      <button 
        v-if="hasRightSidebar"
        id="mobile-right-sidebar-toggle" 
        class="btn btn-link text-body p-0"
        @click="toggleRightSidebar"
      >
        <i class="bi bi-layout-sidebar-reverse"></i>
      </button>
    </div>
  </header>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';

// Emits
interface Emits {
  (e: 'toggle-left-sidebar'): void;
  (e: 'toggle-right-sidebar'): void;
}

const emit = defineEmits<Emits>();

// Router
const route = useRoute();

// Computed
const hasRightSidebar = computed<boolean>(() => {
  return route.meta.hasRightSidebar ?? false;
});

const headerClass = computed<string>(() => {
  // Show header up to xl breakpoint if there's a right sidebar, otherwise up to lg
  return hasRightSidebar.value ? 'd-xl-none' : 'd-lg-none';
});

const pageTitle = computed<string>(() => {
  // Get title from route meta or use route name
  return (route.meta.title as string) || formatRouteName(route.name as string);
});

// Methods
function formatRouteName(name: string | undefined): string {
  if (!name) return 'Medley';
  
  // Convert route names to readable titles
  const titleMap: Record<string, string> = {
    'dashboard': 'Dashboard',
    'articles': 'Articles',
    'sources': 'Sources',
    'fragments': 'Fragments',
    'ai-prompts': 'AI Prompts',
    'speakers': 'Speakers'
  };
  
  return titleMap[name] || name.charAt(0).toUpperCase() + name.slice(1);
}

function toggleLeftSidebar(e: Event): void {
  e.stopPropagation();
  emit('toggle-left-sidebar');
}

function toggleRightSidebar(e: Event): void {
  e.stopPropagation();
  emit('toggle-right-sidebar');
}
</script>
