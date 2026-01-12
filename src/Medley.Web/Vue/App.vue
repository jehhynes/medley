<template>
  <div 
    class="main-page-container"
    :class="{
      'has-left-sidebar': hasLeftSidebar,
      'has-right-sidebar': hasRightSidebar
    }"
  >
    <router-view />
  </div>
</template>

<script>
export default {
  name: 'App',
  computed: {
    hasLeftSidebar() {
      return this.$route.meta.hasLeftSidebar ?? true;
    },
    hasRightSidebar() {
      return this.$route.meta.hasRightSidebar ?? false;
    },
    openSidebarOnMobile() {
      // Dashboard should never open sidebar on mobile
      if (this.$route.name === 'dashboard') {
        return false;
      }
      
      // Open sidebar on mobile if there's no ID in the route (query or params)
      const hasId = this.$route.params.id || this.$route.query.id;
      return !hasId;
    }
  },
  watch: {
    openSidebarOnMobile: {
      immediate: true,
      handler(shouldOpen) {
        // Update the global state for mobile sidebar
        if (window.MedleyUser) {
          window.MedleyUser.openSidebarOnMobile = shouldOpen;
        }
        
        // Trigger mobile sidebar state update
        this.$nextTick(() => {
          const leftSidebar = document.querySelector('.left-sidebar');
          const verticalMenu = document.querySelector('.vertical-menu');
          
          if (shouldOpen) {
            if (leftSidebar) leftSidebar.classList.add('show');
            if (verticalMenu) verticalMenu.classList.add('show');
          } else {
            if (leftSidebar) leftSidebar.classList.remove('show');
            if (verticalMenu) verticalMenu.classList.remove('show');
          }
        });
      }
    }
  }
};
</script>
