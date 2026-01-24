<template>
  <div v-if="visible && fragment">
    <div class="modal fade show" id="fragmentModal" tabindex="-1" aria-labelledby="fragmentModalLabel" style="display: block;" @click.self="handleBackdropClick">
      <div class="modal-dialog modal-xl">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title" id="fragmentModalLabel">
              {{ fragment.title || 'Untitled Fragment' }}
            </h5>
            <button type="button" class="btn-close" @click="close" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            <fragment-body 
              :fragment="fragment" 
              @updated="handleFragmentUpdated"
              @deleted="handleFragmentDeleted"
              ref="fragmentBodyRef"
            />
          </div>
        </div>
      </div>
    </div>
    <div class="modal-backdrop fade show"></div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import FragmentBody from './FragmentBody.vue';
import type { FragmentDto } from '@/types/api-client';

// Props
interface Props {
  fragment: FragmentDto | null;
  visible: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  fragment: null,
  visible: false
});

// Emits
interface Emits {
  (e: 'close'): void;
  (e: 'updated', fragment: FragmentDto): void;
  (e: 'deleted', fragmentId: string): void;
}

const emit = defineEmits<Emits>();

// Refs
const fragmentBodyRef = ref<InstanceType<typeof FragmentBody> | null>(null);

// Watchers
watch(() => props.visible, (newVal) => {
  if (!newVal) {
    // Reset state when modal closes
  }
});

// Methods
function close(): void {
  // Check if there are unsaved changes in the fragment body
  if (fragmentBodyRef.value?.hasUnsavedChanges()) {
    if (!confirm('You have unsaved changes. Are you sure you want to close?')) {
      return;
    }
  }
  emit('close');
}

function handleBackdropClick(): void {
  close();
}

function handleFragmentUpdated(updatedFragment: FragmentDto): void {
  emit('updated', updatedFragment);
}

function handleFragmentDeleted(fragmentId: string): void {
  // Close modal and emit deleted event
  emit('deleted', fragmentId);
  emit('close');
}

function handleKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape' && props.visible) {
    close();
  }
}

// Lifecycle
import { onMounted, onBeforeUnmount } from 'vue';

onMounted(() => {
  window.addEventListener('keydown', handleKeydown);
});

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleKeydown);
});
</script>
