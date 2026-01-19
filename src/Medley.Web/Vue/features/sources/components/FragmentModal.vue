<template>
  <div v-if="visible && fragment">
    <div class="modal fade show" id="fragmentModal" tabindex="-1" aria-labelledby="fragmentModalLabel" style="display: block;" @click.self="close">
      <div class="modal-dialog modal-xl">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title" id="fragmentModalLabel">
              {{ fragment.title || 'Untitled Fragment' }}
            </h5>
            <button type="button" class="btn-close" @click="close" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            <div class="mb-3">
              <span v-if="fragment.category" class="badge bg-secondary me-2">
                <i :class="getIconClass(getFragmentCategoryIcon(fragment.category))" class="me-1"></i>{{ fragment.category }}
              </span>
              <span 
                v-if="fragment.confidence !== null && fragment.confidence !== undefined && fragment.confidenceComment" 
                class="badge bg-light text-dark"
                @click="toggleConfidenceComment"
                style="cursor: pointer;"
                :title="showConfidenceComment ? 'Hide confidence note' : 'Show confidence note'">
                <i 
                  :class="'fa-duotone ' + getConfidenceIcon(fragment.confidence)" 
                  :style="{ color: getConfidenceColor(fragment.confidence) }"
                  class="me-1"
                ></i>
                Confidence: {{ fragment.confidence || '' }}
                <i :class="showConfidenceComment ? 'bi bi-chevron-up ms-1' : 'bi bi-chevron-down ms-1'"></i>
              </span>
              <span 
                v-else-if="fragment.confidence !== null && fragment.confidence !== undefined" 
                class="badge bg-light text-dark">
                <i 
                  :class="'fa-duotone ' + getConfidenceIcon(fragment.confidence)" 
                  :style="{ color: getConfidenceColor(fragment.confidence) }"
                  class="me-1"
                ></i>
                Confidence: {{ fragment.confidence || '' }}
              </span>
            </div>
            <div v-if="fragment.confidenceComment && showConfidenceComment" class="alert alert-info mb-3">
              <div class="d-flex align-items-start">
                <i class="bi bi-info-circle me-2 mt-1"></i>
                <div>
                  <strong>Confidence Note:</strong>
                  <div class="mt-1">{{ fragment.confidenceComment }}</div>
                </div>
              </div>
            </div>
            <div class="markdown-container" v-html="renderedMarkdown"></div>
          </div>
        </div>
      </div>
    </div>
    <div class="modal-backdrop fade show"></div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue';
import { 
  getIconClass, 
  getFragmentCategoryIcon, 
  getConfidenceIcon, 
  getConfidenceColor
} from '@/utils/helpers';
import type { FragmentDto } from '@/types/api-client';

// Declare marked as global
declare const marked: {
  parse: (markdown: string) => string;
};

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
}

const emit = defineEmits<Emits>();

// State
const showConfidenceComment = ref<boolean>(false);

// Computed
const renderedMarkdown = computed<string>(() => {
  if (!props.fragment || !props.fragment.content) {
    return '';
  }
  if (typeof marked !== 'undefined') {
    return marked.parse(props.fragment.content);
  }
  return props.fragment.content.replace(/\n/g, '<br>');
});

// Watchers
watch(() => props.visible, (newVal) => {
  if (!newVal) {
    showConfidenceComment.value = false;
  }
});

// Methods
function close(): void {
  emit('close');
}

function toggleConfidenceComment(): void {
  showConfidenceComment.value = !showConfidenceComment.value;
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
