<template>
  <div class="tool-call-item text-muted">
    <div class="d-flex align-items-center">
      <!-- Cursor tool with custom SVG icon -->
      <template v-if="tool.name && tool.name.toLowerCase().includes('cursor')">
        <img :src="cursorIcon" 
             class="svg-icon me-1" 
             style="width: 19px; height: 19px;"
             :title="formatToolName(tool.name)" 
             alt="Cursor AI" />
      </template>
      <i v-else class="bi me-2" :class="getToolIcon(tool.name)" :title="formatToolName(tool.name)"></i>
      
      <!-- CreatePlan tool with link -->
      <template v-if="tool.name && tool.name.toLowerCase().includes('createplan') && tool.completed && getIdFromResult(tool.result)">
        <a href="#" 
           @click.prevent="$emit('open-plan', getIdFromResult(tool.result))"
           class="tool-call-link">
          {{ tool.display || formatToolName(tool.name) }}
        </a>
      </template>
      
      <!-- GetKnowledgeUnitContent tool with link -->
      <template v-else-if="tool.name && tool.name.toLowerCase().includes('getknowledgeunitcontent') && tool.completed && getIdFromResult(tool.result)">
        <a href="#" 
           @click.prevent="$emit('open-knowledge-unit', getIdFromResult(tool.result))"
           class="tool-call-link">
          {{ tool.display || formatToolName(tool.name) }}
        </a>
      </template>
      
      <!-- CreateArticleVersion tool with link -->
      <template v-else-if="tool.name && tool.name.toLowerCase().includes('createarticleversion') && tool.completed && getIdFromResult(tool.result)">
        <a href="#" 
           @click.prevent="$emit('open-version', getIdFromResult(tool.result))"
           class="tool-call-link">
          {{ tool.display || formatToolName(tool.name) }}
        </a>
      </template>
      
      <!-- Cursor tool with clickable link when completed -->
      <template v-else-if="tool.name && tool.name.toLowerCase().includes('cursor') && tool.completed">
        <a href="#"
           @click.prevent="showCursorResponse"
           class="tool-call-link">
          {{ tool.display || formatToolName(tool.name) }}
        </a>
      </template>
      
      <!-- Multi-result tools (Search/FindSimilar) with expansion -->
      <template v-else-if="isMultiResultTool(tool.name) && tool.completed">
        <a href="#"
           @click.prevent="toggleExpansion"
           class="tool-call-link">
          {{ tool.display || formatToolName(tool.name) }}
          <template v-if="tool.result && tool.result.ids"> ({{ tool.result.ids.length }})</template>
        </a>
      </template>
      
      <!-- Default tool display -->
      <span v-else class="tool-call-text">{{ tool.display || formatToolName(tool.name) }}</span>
      
      <i v-if="tool.isError" class="bi bi-x-circle ms-2 text-danger" title="Tool execution failed"></i>
      <i v-else-if="tool.completed" class="bi bi-check-circle ms-2 text-success"></i>
      <span v-else class="spinner-border spinner-border-xs ms-2" role="status"></span>
    </div>
    
    <!-- Expanded knowledge unit list -->
    <div v-if="isExpanded" class="tool-knowledge-units-list ms-4">
      <div v-if="!knowledgeUnits" class="text-muted small">
        <span class="spinner-border spinner-border-xs me-1" role="status"></span>
        Loading knowledge units...
      </div>
      <div v-else-if="knowledgeUnits.length === 0" class="text-muted small">
        No knowledge units found
      </div>
      <div v-else>
        <a v-for="knowledgeUnit in knowledgeUnits" 
           :key="knowledgeUnit.id"
           href="#"
           @click.prevent="$emit('open-knowledge-unit', knowledgeUnit.id)"
           class="d-block text-decoration-none text-muted small mb-1 knowledge-unit-link">
          <i class="bi bi-puzzle me-1"></i>{{ knowledgeUnit.title || 'Untitled Knowledge Unit' }}
        </a>
      </div>
    </div>
  </div>
  
  <!-- Cursor Response Modal -->
  <cursor-response-modal
    v-if="cursorModalVisible"
    :tool-name="formatToolName(tool.name)"
    :article-id="articleId"
    :conversation-id="conversationId"
    :message-id="messageId"
    :tool-call-id="tool.callId"
    @close="closeCursorModal" />
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { apiClients } from '@/utils/apiClients';
import type { KnowledgeUnitTitleDto } from '@/types/api-client';
import CursorResponseModal from './CursorResponseModal.vue';
import cursorIcon from '@/../wwwroot/images/cursor-ai.svg?url';

interface ToolResult {
  ids?: string[];
  [key: string]: any;
}

interface Tool {
  name: string;
  display?: string;
  completed: boolean;
  isError?: boolean;
  result?: ToolResult;
  callId?: string;
}

interface Props {
  tool: Tool;
  articleId?: string;
  conversationId?: string;
  messageId?: string;
}

const props = defineProps<Props>();

interface Emits {
  (e: 'open-plan', planId: string): void;
  (e: 'open-knowledge-unit', knowledgeUnitId: string): void;
  (e: 'open-version', versionId: string): void;
}

const emit = defineEmits<Emits>();

const isExpanded = ref<boolean>(false);
const knowledgeUnits = ref<KnowledgeUnitTitleDto[] | null>(null);
const cursorModalVisible = ref<boolean>(false);

function formatToolName(toolName: string): string {
  if (!toolName) return '';
  
  let words = toolName.split('_');
  
  words = words.flatMap(word => {
    return word.replace(/([A-Z])/g, ' $1').trim().split(/\s+/);
  });
  
  return words
    .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
    .join(' ');
}

function getToolIcon(toolName: string): string {
  if (!toolName) return 'bi-gear';
  
  const lowerName = toolName.toLowerCase();
  if (lowerName.includes('search') || lowerName.includes('findsimilar')) {
    return 'bi-search';
  }
  if (lowerName.includes('knowledgeunit') || lowerName.includes('content')) {
    return 'bi-puzzle';
  }
  if (lowerName.includes('createplan')) {
    return 'bi-list-check';
  }
  if (lowerName.includes('version') || lowerName.includes('createarticleversion')) {
    return 'bi-file-text';
  }
  return 'bi-gear';
}

function getIdFromResult(result: ToolResult | undefined): string | null {
  if (!result) return null;
  
  try {
    if (result.ids && Array.isArray(result.ids) && result.ids.length > 0) {
      return result.ids[0];
    }
    return null;
  } catch (e) {
    return null;
  }
}

function isMultiResultTool(toolName: string): boolean {
  if (!toolName) return false;
  const lowerName = toolName.toLowerCase();
  return lowerName.includes('search') || lowerName.includes('findsimilar');
}

async function toggleExpansion(): Promise<void> {
  isExpanded.value = !isExpanded.value;
  
  if (isExpanded.value && knowledgeUnits.value === null) {
    await loadKnowledgeUnits();
  }
}

async function loadKnowledgeUnits(): Promise<void> {
  if (!props.tool.result || !props.tool.result.ids || props.tool.result.ids.length === 0) {
    knowledgeUnits.value = [];
    return;
  }

  try {
    knowledgeUnits.value = await apiClients.knowledgeUnits.getTitles(props.tool.result.ids);
  } catch (err) {
    console.error('Error loading tool knowledge units:', err);
    knowledgeUnits.value = [];
  }
}

async function showCursorResponse(): Promise<void> {
  cursorModalVisible.value = true;
}

function closeCursorModal(): void {
  cursorModalVisible.value = false;
}
</script>
