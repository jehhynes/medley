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

<script>
export default {
  name: 'MobileHeader',
  computed: {
    hasRightSidebar() {
      return this.$route.meta.hasRightSidebar ?? false;
    },
    headerClass() {
      // Show header up to xl breakpoint if there's a right sidebar, otherwise up to lg
      return this.hasRightSidebar ? 'd-xl-none' : 'd-lg-none';
    },
    pageTitle() {
      // Get title from route meta or use route name
      return this.$route.meta.title || this.formatRouteName(this.$route.name);
    }
  },
  methods: {
    formatRouteName(name) {
      if (!name) return 'Medley';
      
      // Convert route names to readable titles
      const titleMap = {
        'dashboard': 'Dashboard',
        'articles': 'Articles',
        'sources': 'Sources',
        'fragments': 'Fragments',
        'ai-prompts': 'AI Prompts'
      };
      
      return titleMap[name] || name.charAt(0).toUpperCase() + name.slice(1);
    },
    toggleLeftSidebar(e) {
      e.stopPropagation();
      this.$emit('toggle-left-sidebar');
    },
    toggleRightSidebar(e) {
      e.stopPropagation();
      this.$emit('toggle-right-sidebar');
    }
  }
};
</script>
