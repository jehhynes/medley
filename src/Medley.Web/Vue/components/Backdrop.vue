<template>
  <div 
    class="mobile-sidebar-backdrop"
    :class="backdropClass"
    @click="handleClick"
    aria-hidden="true"
  ></div>
</template>

<script setup lang="ts">
// Props
interface Props {
  isAutomatic?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  isAutomatic: false
});

// Emits
interface Emits {
  (e: 'click'): void;
}

const emit = defineEmits<Emits>();

// Computed
const backdropClass = computed<string>(() => {
  // Use 'show-initial' for automatic opening (mobile-only backdrop)
  // Use 'show' for manual toggle (backdrop on all sizes up to xl)
  return props.isAutomatic ? 'show-initial' : 'show';
});

// Methods
function handleClick(): void {
  emit('click');
}
</script>