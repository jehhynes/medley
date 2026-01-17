import { ref, computed, type Ref, type ComputedRef } from 'vue';

/**
 * Return type for useSidebarState composable
 */
interface UseSidebarStateReturn {
  leftSidebarVisible: Ref<boolean>;
  rightSidebarVisible: Ref<boolean>;
  backdropVisible: ComputedRef<boolean>;
  anySidebarVisible: ComputedRef<boolean>;
  isAutomaticOpen: Ref<boolean>;
  showLeftSidebar: (automatic?: boolean) => void;
  hideLeftSidebar: () => void;
  toggleLeftSidebar: () => void;
  showRightSidebar: () => void;
  hideRightSidebar: () => void;
  toggleRightSidebar: () => void;
  hideAllSidebars: () => void;
}

// Shared reactive state for sidebar visibility
const leftSidebarVisible = ref<boolean>(false);
const rightSidebarVisible = ref<boolean>(false);
// Track whether sidebar was opened automatically (route change) vs manually (user click)
const isAutomaticOpen = ref<boolean>(false);

/**
 * Composable for managing sidebar visibility state across the application.
 * Provides centralized state management for left and right sidebars,
 * along with methods to show, hide, and toggle their visibility.
 * 
 * @returns Sidebar state and control methods
 */
export function useSidebarState(): UseSidebarStateReturn {
  // Computed property: backdrop is visible if any sidebar is visible
  const backdropVisible = computed(() => 
    leftSidebarVisible.value || rightSidebarVisible.value
  );

  // Computed property: check if any sidebar is visible
  const anySidebarVisible = computed(() => 
    leftSidebarVisible.value || rightSidebarVisible.value
  );

  // Left sidebar methods
  const showLeftSidebar = (automatic: boolean = false): void => {
    leftSidebarVisible.value = true;
    isAutomaticOpen.value = automatic;
  };

  const hideLeftSidebar = (): void => {
    leftSidebarVisible.value = false;
  };

  const toggleLeftSidebar = (): void => {
    leftSidebarVisible.value = !leftSidebarVisible.value;
    // Manual toggle is never automatic
    isAutomaticOpen.value = false;
    // Close right sidebar when opening left
    if (leftSidebarVisible.value) {
      rightSidebarVisible.value = false;
    }
  };

  // Right sidebar methods
  const showRightSidebar = (): void => {
    rightSidebarVisible.value = true;
    // Right sidebar is always manual
    isAutomaticOpen.value = false;
    // Close left sidebar when opening right
    leftSidebarVisible.value = false;
  };

  const hideRightSidebar = (): void => {
    rightSidebarVisible.value = false;
  };

  const toggleRightSidebar = (): void => {
    rightSidebarVisible.value = !rightSidebarVisible.value;
    // Manual toggle is never automatic
    isAutomaticOpen.value = false;
    // Close left sidebar when opening right
    if (rightSidebarVisible.value) {
      leftSidebarVisible.value = false;
    }
  };

  // Hide all sidebars
  const hideAllSidebars = (): void => {
    leftSidebarVisible.value = false;
    rightSidebarVisible.value = false;
  };

  return {
    // State
    leftSidebarVisible,
    rightSidebarVisible,
    backdropVisible,
    anySidebarVisible,
    isAutomaticOpen,

    // Methods
    showLeftSidebar,
    hideLeftSidebar,
    toggleLeftSidebar,
    showRightSidebar,
    hideRightSidebar,
    toggleRightSidebar,
    hideAllSidebars
  };
}
