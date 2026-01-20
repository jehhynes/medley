import { ref, computed, type Ref, type ComputedRef } from 'vue';

const leftSidebarVisible = ref<boolean>(false);
const rightSidebarVisible = ref<boolean>(false);
const isAutomaticOpen = ref<boolean>(false);

/** Manages sidebar visibility state across the application */
export function useSidebarState() {
  const backdropVisible = computed(() => 
    leftSidebarVisible.value || rightSidebarVisible.value
  );

  const anySidebarVisible = computed(() => 
    leftSidebarVisible.value || rightSidebarVisible.value
  );

  const showLeftSidebar = (automatic: boolean = false) => {
    leftSidebarVisible.value = true;
    isAutomaticOpen.value = automatic;
  };

  const hideLeftSidebar = () => {
    leftSidebarVisible.value = false;
  };

  const toggleLeftSidebar = () => {
    leftSidebarVisible.value = !leftSidebarVisible.value;
    isAutomaticOpen.value = false;
    if (leftSidebarVisible.value) {
      rightSidebarVisible.value = false;
    }
  };

  const showRightSidebar = () => {
    rightSidebarVisible.value = true;
    isAutomaticOpen.value = false;
    leftSidebarVisible.value = false;
  };

  const hideRightSidebar = () => {
    rightSidebarVisible.value = false;
  };

  const toggleRightSidebar = () => {
    rightSidebarVisible.value = !rightSidebarVisible.value;
    isAutomaticOpen.value = false;
    if (rightSidebarVisible.value) {
      leftSidebarVisible.value = false;
    }
  };

  const hideAllSidebars = () => {
    leftSidebarVisible.value = false;
    rightSidebarVisible.value = false;
  };

  return {
    leftSidebarVisible,
    rightSidebarVisible,
    backdropVisible,
    anySidebarVisible,
    isAutomaticOpen,
    showLeftSidebar,
    hideLeftSidebar,
    toggleLeftSidebar,
    showRightSidebar,
    hideRightSidebar,
    toggleRightSidebar,
    hideAllSidebars
  };
}
