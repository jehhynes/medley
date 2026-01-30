<template>
  <div v-if="visible && knowledgeUnit">
    <div class="modal fade show" id="knowledgeUnitModal" tabindex="-1" aria-labelledby="knowledgeUnitModalLabel" style="display: block;" @click.self="handleBackdropClick">
      <div class="modal-dialog modal-xl">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title" id="knowledgeUnitModalLabel">
              {{ knowledgeUnit.title || 'Untitled Knowledge Unit' }}
            </h5>
            <button type="button" class="btn-close" @click="close" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            <knowledge-unit-body 
              :knowledge-unit="knowledgeUnit" 
              @updated="handleKnowledgeUnitUpdated"
              @deleted="handleKnowledgeUnitDeleted"
              ref="knowledgeUnitBodyRef"
            />
          </div>
        </div>
      </div>
    </div>
    <div class="modal-backdrop fade show"></div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted, onBeforeUnmount } from 'vue';
import KnowledgeUnitBody from '../../sources/components/KnowledgeUnitBody.vue';
import type { KnowledgeUnitDto } from '@/types/api-client';

// Props
interface Props {
  knowledgeUnit: KnowledgeUnitDto | null;
  visible: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  knowledgeUnit: null,
  visible: false
});

// Emits
interface Emits {
  (e: 'close'): void;
  (e: 'updated', knowledgeUnit: KnowledgeUnitDto): void;
  (e: 'deleted', knowledgeUnitId: string): void;
}

const emit = defineEmits<Emits>();

// Refs
const knowledgeUnitBodyRef = ref<InstanceType<typeof KnowledgeUnitBody> | null>(null);

// Watchers
watch(() => props.visible, (newVal) => {
  if (!newVal) {
    // Reset state when modal closes
  }
});

// Methods
function close(): void {
  // Check if there are unsaved changes in the knowledge unit body
  if (knowledgeUnitBodyRef.value?.hasUnsavedChanges && knowledgeUnitBodyRef.value?.hasUnsavedChanges()) {
    if (!confirm('You have unsaved changes. Are you sure you want to close?')) {
      return;
    }
  }
  emit('close');
}

function handleBackdropClick(): void {
  close();
}

function handleKnowledgeUnitUpdated(updatedKnowledgeUnit: KnowledgeUnitDto): void {
  emit('updated', updatedKnowledgeUnit);
}

function handleKnowledgeUnitDeleted(knowledgeUnitId: string): void {
  // Close modal and emit deleted event
  emit('deleted', knowledgeUnitId);
  emit('close');
}

function handleKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape' && props.visible) {
    close();
  }
}

// Lifecycle
onMounted(() => {
  window.addEventListener('keydown', handleKeydown);
});

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleKeydown);
});
</script>
