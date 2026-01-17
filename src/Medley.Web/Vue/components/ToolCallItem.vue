<template>
  <div class="tool-call-item text-muted">
    <div class="d-flex align-items-center">
      <i class="bi me-2" :class="getToolIcon(tool.name)" :title="formatToolName(tool.name)"></i>
      
      <!-- CreatePlan tool with link -->
      <template v-if="tool.name && tool.name.toLowerCase().includes('createplan') && tool.completed && getIdFromResult(tool.result)">
        <a href="#" 
           @click.prevent="$emit('open-plan', getIdFromResult(tool.result))"
           class="tool-call-link">
          {{ tool.display || formatToolName(tool.name) }}
        </a>
      </template>
      
      <!-- GetFragmentContent tool with link -->
      <template v-else-if="tool.name && tool.name.toLowerCase().includes('getfragmentcontent') && tool.completed && getIdFromResult(tool.result)">
        <a href="#" 
           @click.prevent="$emit('open-fragment', getIdFromResult(tool.result))"
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
    
    <!-- Expanded fragment list -->
    <div v-if="isExpanded" class="tool-fragments-list ms-4">
      <div v-if="!fragments" class="text-muted small">
        <span class="spinner-border spinner-border-xs me-1" role="status"></span>
        Loading fragments...
      </div>
      <div v-else-if="fragments.length === 0" class="text-muted small">
        No fragments found
      </div>
      <div v-else>
        <a v-for="fragment in fragments" 
           :key="fragment.id"
           href="#"
           @click.prevent="$emit('open-fragment', fragment.id)"
           class="d-block text-decoration-none text-muted small mb-1 fragment-link">
          <i class="bi bi-puzzle me-1"></i>{{ fragment.title || 'Untitled Fragment' }}
        </a>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import type { FragmentTitleDto } from '@/types/generated/api-client';

// Tool result interface
interface ToolResult {
  ids?: string[];
  [key: string]: any;
}

// Tool interface
interface Tool {
  name: string;
  display?: string;
  completed: boolean;
  isError?: boolean;
  result?: ToolResult;
}

// Props
interface Props {
  tool: Tool;
}

const props = defineProps<Props>();

// Emits
interface Emits {
  (e: 'open-plan', planId: string): void;
  (e: 'open-fragment', fragmentId: string): void;
  (e: 'open-version', versionId: string): void;
}

const emit = defineEmits<Emits>();

// State
const isExpanded = ref<boolean>(false);
const fragments = ref<FragmentTitleDto[] | null>(null);

// Methods
function formatToolName(toolName: string): string {
  if (!toolName) return '';
  
  // First, split on underscores
  let words = toolName.split('_');
  
  // Then split each word on uppercase letters (PascalCase/camelCase)
  words = words.flatMap(word => {
    // Insert space before uppercase letters and split
    return word.replace(/([A-Z])/g, ' $1').trim().split(/\s+/);
  });
  
  // Capitalize first letter of each word
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
  if (lowerName.includes('fragment') || lowerName.includes('content')) {
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
    // Check if result has ids array and return first ID
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
  
  // Load fragments if expanding and not already loaded
  if (isExpanded.value && fragments.value === null) {
    await loadFragments();
  }
}

async function loadFragments(): Promise<void> {
  // If no result or no IDs, set empty array
  if (!props.tool.result || !props.tool.result.ids || props.tool.result.ids.length === 0) {
    fragments.value = [];
    return;
  }

  try {
    // Use the new batch endpoint to load fragment titles
    const response = await fetch('/api/fragments/titles', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(props.tool.result.ids)
    });

    if (!response.ok) {
      throw new Error('Failed to load fragment titles');
    }

    fragments.value = await response.json();
  } catch (err) {
    console.error('Error loading tool fragments:', err);
    fragments.value = [];
  }
}
</script>
