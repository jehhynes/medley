<template>
  <Teleport to="body">
    <div v-if="visible">
      <div class="modal fade show" id="cursorResponseModal" tabindex="-1" aria-labelledby="cursorResponseModalLabel" style="display: block;" @click.self="close">
        <div class="modal-dialog modal-xl modal-dialog-scrollable">
          <div class="modal-content">
            <div class="modal-header">
              <div class="d-flex align-items-center">
                <img v-if="toolName && toolName.toLowerCase().includes('cursor')"
                     :src="cursorIcon" 
                     class="svg-icon me-2" 
                     style="width: 24px; height: 24px;"
                     alt="Cursor AI" />
                <i v-else class="bi bi-gear me-2" style="font-size: 24px;"></i>
                <h5 class="modal-title mb-0" id="cursorResponseModalLabel">
                  {{ modalTitle }}
                </h5>
              </div>
              <button type="button" class="btn-close" @click="close" aria-label="Close"></button>
            </div>
            <div class="modal-body">
              <div class="mb-3">
                <strong class="text-muted">Prompt:</strong>
                <div class="mt-1">{{ displayQuestion }}</div>
              </div>
              <hr />
              <div v-if="isLoading" class="text-center py-4">
                <span class="spinner-border spinner-border-sm me-2" role="status"></span>
                Loading response...
              </div>
              <div v-else-if="error" class="alert alert-danger">
                {{ error }}
              </div>
              <div v-else class="markdown-container" v-html="renderedMarkdown"></div>
            </div>
          </div>
        </div>
      </div>
      <div class="modal-backdrop fade show"></div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, onMounted, onBeforeUnmount, ref, watch } from 'vue';
import { apiClients } from '@/utils/apiClients';
import cursorIcon from '@/../wwwroot/images/cursor-ai.svg?url';

// Declare marked as global
declare const marked: {
  parse: (markdown: string) => string;
};

// Props
interface Props {
  toolName?: string;
  articleId?: string;
  conversationId?: string;
  messageId?: string;
  toolCallId?: string;
  visible?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  visible: true
});

// Emits
interface Emits {
  (e: 'close'): void;
}

const emit = defineEmits<Emits>();

// State
const isLoading = ref<boolean>(false);
const error = ref<string>('');
const responseContent = ref<string>('');
const question = ref<string>('');

// Computed
const displayQuestion = computed<string>(() => {
  return question.value || 'Question';
});

const modalTitle = computed<string>(() => {
  return props.toolName || 'Tool Response';
});

const renderedMarkdown = computed<string>(() => {
  if (!responseContent.value) {
    return '';
  }
  if (typeof marked !== 'undefined') {
    return marked.parse(responseContent.value);
  }
  return responseContent.value.replace(/\n/g, '<br>');
});

// Methods
function close(): void {
  emit('close');
}

function handleKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape' && props.visible) {
    close();
  }
}

async function loadToolResult(): Promise<void> {
  if (!props.articleId || !props.conversationId || !props.messageId || !props.toolCallId) {
    error.value = 'Missing required information to load response';
    return;
  }

  isLoading.value = true;
  error.value = '';
  
  try {
    const result = await apiClients.articleChat.getToolResultContent(
      props.articleId,
      props.conversationId,
      props.messageId,
      props.toolCallId
    );
    
    responseContent.value = result.content || 'No response available';
    question.value = result.question || '';
  } catch (err) {
    console.error('Error loading tool result:', err);
    error.value = 'Error loading response';
  } finally {
    isLoading.value = false;
  }
}

// Lifecycle
onMounted(() => {
  window.addEventListener('keydown', handleKeydown);
  loadToolResult();
});

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleKeydown);
});

// Watch for prop changes to reload
watch(() => props.toolCallId, () => {
  if (props.visible && props.toolCallId) {
    loadToolResult();
  }
});
</script>

