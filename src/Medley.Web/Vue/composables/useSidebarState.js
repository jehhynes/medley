import { ref, computed } from 'vue'

// Shared reactive state for sidebar visibility
const leftSidebarVisible = ref(false)
const rightSidebarVisible = ref(false)
// Track whether sidebar was opened automatically (route change) vs manually (user click)
const isAutomaticOpen = ref(false)

/**
 * Composable for managing sidebar visibility state across the application.
 * Provides centralized state management for left and right sidebars,
 * along with methods to show, hide, and toggle their visibility.
 * 
 * @returns {Object} Sidebar state and control methods
 */
export function useSidebarState() {
  // Computed property: backdrop is visible if any sidebar is visible
  const backdropVisible = computed(() => 
    leftSidebarVisible.value || rightSidebarVisible.value
  )

  // Computed property: check if any sidebar is visible
  const anySidebarVisible = computed(() => 
    leftSidebarVisible.value || rightSidebarVisible.value
  )

  // Left sidebar methods
  const showLeftSidebar = (automatic = false) => {
    leftSidebarVisible.value = true
    isAutomaticOpen.value = automatic
  }

  const hideLeftSidebar = () => {
    leftSidebarVisible.value = false
  }

  const toggleLeftSidebar = () => {
    leftSidebarVisible.value = !leftSidebarVisible.value
    // Manual toggle is never automatic
    isAutomaticOpen.value = false
    // Close right sidebar when opening left
    if (leftSidebarVisible.value) {
      rightSidebarVisible.value = false
    }
  }

  // Right sidebar methods
  const showRightSidebar = () => {
    rightSidebarVisible.value = true
    // Right sidebar is always manual
    isAutomaticOpen.value = false
    // Close left sidebar when opening right
    leftSidebarVisible.value = false
  }

  const hideRightSidebar = () => {
    rightSidebarVisible.value = false
  }

  const toggleRightSidebar = () => {
    rightSidebarVisible.value = !rightSidebarVisible.value
    // Manual toggle is never automatic
    isAutomaticOpen.value = false
    // Close left sidebar when opening right
    if (rightSidebarVisible.value) {
      leftSidebarVisible.value = false
    }
  }

  // Hide all sidebars
  const hideAllSidebars = () => {
    leftSidebarVisible.value = false
    rightSidebarVisible.value = false
  }

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
  }
}
