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

<script>
import { watch } from 'vue'
import { useRoute } from 'vue-router'
import { useSidebarState } from '@/composables/useSidebarState'
import MobileHeader from '@/components/MobileHeader.vue'
import Backdrop from '@/components/Backdrop.vue'

export default {
  name: 'App',
  components: {
    MobileHeader,
    Backdrop
  },
  setup() {
    const route = useRoute()
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
    } = useSidebarState()

    // Watch route changes to manage automatic sidebar visibility
    watch(
      () => [route.name, route.params.id, route.query.id],
      ([routeName, paramsId, queryId]) => {
        // Dashboard should never open sidebar on mobile
        if (routeName === 'dashboard') {
          hideLeftSidebar()
          return
        }
        
        // Check for ID in params or query
        const hasId = paramsId || queryId
        
        // Show left sidebar if no ID present (mark as automatic)
        if (!hasId) {
          showLeftSidebar(true)
        } else {
          hideLeftSidebar()
        }
      },
      { immediate: true }
    )

    return {
      leftSidebarVisible,
      rightSidebarVisible,
      backdropVisible,
      isAutomaticOpen,
      toggleLeftSidebar,
      toggleRightSidebar,
      hideAllSidebars
    }
  },
  computed: {
    hasLeftSidebar() {
      return this.$route.meta.hasLeftSidebar ?? true
    },
    hasRightSidebar() {
      return this.$route.meta.hasRightSidebar ?? false
    },
    containerClasses() {
      return {
        'has-left-sidebar': this.hasLeftSidebar,
        'has-right-sidebar': this.hasRightSidebar
      }
    }
  }
}
</script>
