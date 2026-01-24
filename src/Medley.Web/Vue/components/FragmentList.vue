<template>
  <div class="fragment-list-container">
    <!-- Loading state -->
    <div v-if="loading" class="text-center py-4">
      <div class="spinner-border spinner-border-sm" role="status">
        <span class="visually-hidden">Loading...</span>
      </div>
    </div>
    
    <!-- Error state -->
    <div v-else-if="error" class="alert alert-danger">
      {{ error }}
    </div>
    
    <!-- Content -->
    <template v-else>
      <!-- Extraction message (optional) -->
      <div v-if="showExtractionMessage && extractionMessage && fragments.length === 0" 
           class="alert alert-info mb-3">
        <div class="d-flex align-items-start">
          <i class="bi bi-info-circle me-2 mt-1"></i>
          <div class="flex-grow-1">
            <strong>Extraction Message:</strong>
            <div class="mt-1">{{ extractionMessage }}</div>
          </div>
        </div>
      </div>
      
      <!-- Empty state -->
      <div v-if="fragments.length === 0 && (!showExtractionMessage || !extractionMessage)" class="text-muted">
        {{ emptyMessage }}
      </div>
      
      <!-- Fragment list -->
      <div v-else-if="fragments.length > 0" class="fragment-list">
        <table class="table table-hover">
          <tbody>
            <tr 
              v-for="fragment in fragments" 
              :key="fragment.id"
              class="fragment-item"
              @click="$emit('select-fragment', fragment)"
              style="cursor: pointer;">
              <td class="align-middle" style="width: 50px;">
                <i 
                  :class="getIconClass(fragment.categoryIcon, 'bi-puzzle')" 
                  :title="fragment.category || ''"
                  style="font-size: 1.25rem;">
                </i>
              </td>
              <td>
                <div class="fw-semibold">{{ fragment.title || 'Untitled Fragment' }}</div>
                <div v-if="fragment.summary" class="text-muted small">
                  {{ fragment.summary }}
                </div>
              </td>
              <td class="align-middle text-end" style="width: 100px;" 
                  v-if="fragment.confidence !== null && fragment.confidence !== undefined">
                <i 
                  :class="'fa-duotone ' + getConfidenceIcon(fragment.confidence)" 
                  :style="{ color: getConfidenceColor(fragment.confidence) }"
                  :title="'Confidence: ' + (fragment.confidence || '')"
                  style="font-size: 1.25rem;">
                </i>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { getIconClass, getConfidenceIcon, getConfidenceColor } from '@/utils/helpers';
import type { FragmentDto } from '@/types/api-client';

// Props
interface Props {
  fragments: FragmentDto[];
  loading?: boolean;
  error?: string | null;
  emptyMessage?: string;
  showExtractionMessage?: boolean;
  extractionMessage?: string | null;
}

withDefaults(defineProps<Props>(), {
  loading: false,
  error: null,
  emptyMessage: 'No fragments available',
  showExtractionMessage: false,
  extractionMessage: null
});

// Events
defineEmits<{
  'select-fragment': [fragment: FragmentDto];
}>();
</script>

<style scoped>
.fragment-list-container {
  width: 100%;
}

.fragment-list {
  width: 100%;
}

.fragment-list .fragment-item:hover {
  background-color: var(--bs-secondary-bg);
}
</style>
