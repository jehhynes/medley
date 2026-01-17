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

<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount, nextTick } from 'vue';

// Scroll state interface
interface ScrollState {
  offsetY: number;
  offsetBottom: number;
  startIndex: number;
  endIndex: number;
}

// Props
interface Props {
  items: any[];
  itemHeight?: number;
  buffer?: number;
  keyField?: string;
}

const props = withDefaults(defineProps<Props>(), {
  items: () => [],
  itemHeight: 56,
  buffer: 5,
  keyField: 'id'
});

// Refs
const scrollContainer = ref<HTMLElement | null>(null);
const scrollState = ref<ScrollState>({
  offsetY: 0,
  offsetBottom: 0,
  startIndex: 0,
  endIndex: 0
});
const scrollTimeout = ref<number | null>(null);
let resizeObserver: ResizeObserver | null = null;

// Computed
const visibleItems = computed(() => {
  return props.items.slice(scrollState.value.startIndex, scrollState.value.endIndex);
});

// Methods
function updateVisibleRange(): void {
  if (!scrollContainer.value) return;
  
  const scrollTop = scrollContainer.value.scrollTop;
  const containerHeight = scrollContainer.value.clientHeight;
  const totalItems = props.items.length;
  
  // If container has no height (hidden), skip calculation
  // ResizeObserver will trigger recalculation when it becomes visible
  if (containerHeight === 0) return;
  
  // Calculate which items should be visible
  const startIndex = Math.max(0, Math.floor(scrollTop / props.itemHeight) - props.buffer);
  const visibleCount = Math.ceil(containerHeight / props.itemHeight) + (props.buffer * 2);
  const endIndex = Math.min(totalItems, startIndex + visibleCount);
  
  // Update scroll state
  scrollState.value.startIndex = startIndex;
  scrollState.value.endIndex = endIndex;
  scrollState.value.offsetY = startIndex * props.itemHeight;
  scrollState.value.offsetBottom = (totalItems - endIndex) * props.itemHeight;
}

function handleScroll(): void {
  if (scrollTimeout.value) {
    cancelAnimationFrame(scrollTimeout.value);
  }
  scrollTimeout.value = requestAnimationFrame(() => {
    updateVisibleRange();
  });
}

function scrollToIndex(index: number, behavior: ScrollBehavior = 'auto'): void {
  if (!scrollContainer.value) return;
  const scrollTop = index * props.itemHeight;
  scrollContainer.value.scrollTo({
    top: scrollTop,
    behavior: behavior
  });
}

function getScrollContainer(): HTMLElement | null {
  return scrollContainer.value;
}

// Watchers
watch(() => props.items, () => {
  nextTick(() => {
    updateVisibleRange();
  });
}, { immediate: true });

watch(() => props.itemHeight, () => {
  nextTick(() => {
    updateVisibleRange();
  });
});

// Lifecycle
onMounted(() => {
  if (scrollContainer.value) {
    scrollContainer.value.addEventListener('scroll', handleScroll);
    // Initial calculation
    updateVisibleRange();
    
    // Set up ResizeObserver to detect when container becomes visible or resizes
    resizeObserver = new ResizeObserver(() => {
      // When container size changes (e.g., becoming visible), recalculate
      updateVisibleRange();
    });
    resizeObserver.observe(scrollContainer.value);
  }
});

onBeforeUnmount(() => {
  if (scrollContainer.value) {
    scrollContainer.value.removeEventListener('scroll', handleScroll);
  }
  if (resizeObserver) {
    resizeObserver.disconnect();
  }
  if (scrollTimeout.value) {
    cancelAnimationFrame(scrollTimeout.value);
  }
});

// Expose methods for parent components
defineExpose({
  scrollToIndex,
  getScrollContainer
});
</script>
