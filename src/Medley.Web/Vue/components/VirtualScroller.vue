<template>
  <div class="virtual-scroller" ref="scrollContainer">
    <div class="virtual-scroller-spacer" :style="{ height: scrollState.offsetY + 'px' }"></div>
    <slot 
      name="item" 
      v-for="(item, index) in visibleItems" 
      :item="item" 
      :index="scrollState.startIndex + index"
      :key="keyField ? item[keyField] : index">
    </slot>
    <div class="virtual-scroller-spacer" :style="{ height: scrollState.offsetBottom + 'px' }"></div>
  </div>
</template>

<script>
export default {
  name: 'VirtualScroller',
  props: {
    // Array of items to render
    items: {
      type: Array,
      default: () => []
    },
    // Height of each item in pixels
    itemHeight: {
      type: Number,
      default: 56
    },
    // Number of extra items to render above/below viewport for smooth scrolling
    buffer: {
      type: Number,
      default: 5
    },
    // Key field for v-for key binding (e.g., 'id')
    keyField: {
      type: String,
      default: 'id'
    }
  },
  data() {
    return {
      scrollState: {
        offsetY: 0,
        offsetBottom: 0,
        startIndex: 0,
        endIndex: 0
      },
      scrollTimeout: null
    };
  },
  computed: {
    visibleItems() {
      return this.items.slice(this.scrollState.startIndex, this.scrollState.endIndex);
    }
  },
  methods: {
    /**
     * Update visible range based on scroll position
     * This is the core of virtual scrolling - only render items in viewport
     */
    updateVisibleRange() {
      if (!this.$refs.scrollContainer) return;
      
      const scrollTop = this.$refs.scrollContainer.scrollTop;
      const containerHeight = this.$refs.scrollContainer.clientHeight;
      const totalItems = this.items.length;
      
      // If container has no height (hidden), skip calculation
      // ResizeObserver will trigger recalculation when it becomes visible
      if (containerHeight === 0) return;
      
      // Calculate which items should be visible
      const startIndex = Math.max(0, Math.floor(scrollTop / this.itemHeight) - this.buffer);
      const visibleCount = Math.ceil(containerHeight / this.itemHeight) + (this.buffer * 2);
      const endIndex = Math.min(totalItems, startIndex + visibleCount);
      
      // Update scroll state
      this.scrollState.startIndex = startIndex;
      this.scrollState.endIndex = endIndex;
      this.scrollState.offsetY = startIndex * this.itemHeight;
      this.scrollState.offsetBottom = (totalItems - endIndex) * this.itemHeight;
    },
    /**
     * Handle scroll event - debounced for performance using requestAnimationFrame
     */
    handleScroll() {
      if (this.scrollTimeout) {
        cancelAnimationFrame(this.scrollTimeout);
      }
      this.scrollTimeout = requestAnimationFrame(() => {
        this.updateVisibleRange();
      });
    },
    /**
     * Scroll to a specific item by index
     * @param {number} index - Index of item to scroll to
     * @param {string} behavior - Scroll behavior ('auto' or 'smooth')
     */
    scrollToIndex(index, behavior = 'auto') {
      if (!this.$refs.scrollContainer) return;
      const scrollTop = index * this.itemHeight;
      this.$refs.scrollContainer.scrollTo({
        top: scrollTop,
        behavior: behavior
      });
    },
    /**
     * Get the current scroll container element
     * Useful for parent components that need direct access
     */
    getScrollContainer() {
      return this.$refs.scrollContainer;
    }
  },
  watch: {
    items: {
      handler() {
        // Recalculate visible range when items change
        this.$nextTick(() => {
          this.updateVisibleRange();
        });
      },
      immediate: true
    },
    itemHeight() {
      // Recalculate if item height changes
      this.$nextTick(() => {
        this.updateVisibleRange();
      });
    }
  },
  mounted() {
    // Set up scroll listener
    if (this.$refs.scrollContainer) {
      this.$refs.scrollContainer.addEventListener('scroll', this.handleScroll);
      // Initial calculation
      this.updateVisibleRange();
      
      // Set up ResizeObserver to detect when container becomes visible or resizes
      this.resizeObserver = new ResizeObserver(() => {
        // When container size changes (e.g., becoming visible), recalculate
        this.updateVisibleRange();
      });
      this.resizeObserver.observe(this.$refs.scrollContainer);
    }
  },
  beforeUnmount() {
    // Clean up
    if (this.$refs.scrollContainer) {
      this.$refs.scrollContainer.removeEventListener('scroll', this.handleScroll);
    }
    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
    }
    if (this.scrollTimeout) {
      cancelAnimationFrame(this.scrollTimeout);
    }
  }
};
</script>
