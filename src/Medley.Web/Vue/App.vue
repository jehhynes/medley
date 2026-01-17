<template>
  <div 
    class="main-page-container"
    :class="containerClasses"
  >
    <mobile-header 
      @toggle-left-sidebar="toggleLeftSidebar"
      @toggle-right-sidebar="toggleRightSidebar"
    />
    <backdrop 
      v-if="backdropVisible"
      :is-automatic="isAutomaticOpen"
      @click="hideAllSidebars"
    />
    <router-view />
  </div>
</template>

<script setup lang="ts">
import { watch, computed } from 'vue';
import { useRoute } from 'vue-router';
import { useSidebarState } from '@/composables/useSidebarState';
import MobileHeader from '@/components/MobileHeader.vue';
import Backdrop from '@/components/Backdrop.vue';

// Route meta interface
interface RouteMeta {
  hasLeftSidebar?: boolean;
  hasRightSidebar?: boolean;
}

const route = useRoute();
const {
  leftSidebarVisible,
  rightSidebarVisible,
  backdropVisible,
  isAutomaticOpen,
  showLeftSidebar,
  hideLeftSidebar,
  toggleLeftSidebar,
  toggleRightSidebar,
  hideAllSidebars
} = useSidebarState();

// Watch route changes to manage automatic sidebar visibility
watch(
  () => [route.name, route.params.id, route.query.id] as const,
  ([routeName, paramsId, queryId]) => {
    // Dashboard should never open sidebar on mobile
    if (routeName === 'dashboard') {
      hideLeftSidebar();
      return;
    }
    
    // Check for ID in params or query
    const hasId = paramsId || queryId;
    
    // Show left sidebar if no ID present (mark as automatic)
    if (!hasId) {
      showLeftSidebar(true);
    } else {
      hideLeftSidebar();
    }
  },
  { immediate: true }
);

// Computed properties
const hasLeftSidebar = computed<boolean>(() => {
  const meta = route.meta as RouteMeta;
  return meta.hasLeftSidebar ?? true;
});

const hasRightSidebar = computed<boolean>(() => {
  const meta = route.meta as RouteMeta;
  return meta.hasRightSidebar ?? false;
});

const containerClasses = computed(() => ({
  'has-left-sidebar': hasLeftSidebar.value,
  'has-right-sidebar': hasRightSidebar.value
}));
</script>
