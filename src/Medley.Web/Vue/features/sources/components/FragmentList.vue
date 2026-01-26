<template>
  <ul class="list-view">
    <li v-for="fragment in fragments" :key="fragment.id" class="list-item">
      <a href="#" 
         class="list-item-content" 
         :class="{ active: selectedId === fragment.id }"
         @click.prevent="selectFragment(fragment)">
        <i class="list-item-icon" :class="getIconClass(fragment.categoryIcon, 'bi-puzzle')"></i>
        <div class="list-item-body">
          <div class="list-item-title">{{ fragment.title || 'Untitled Fragment' }}</div>
          <div class="list-item-subtitle">
            {{ formatDate(fragment.sourceDate) }} • {{ fragment.sourceType }}
            <span v-if="fragment.similarity !== undefined" class="badge bg-info ms-2" :title="'Similarity: ' + (fragment.similarity * 100).toFixed(1) + '%'">
              {{ (fragment.similarity * 100).toFixed(0) }}%
            </span>
            <span v-if="fragment.primarySpeaker">
              • {{ fragment.primarySpeaker.name }}
              <i 
                v-if="fragment.primarySpeaker.trustLevel" 
                class="bi bi-shield-check ms-1" 
                :class="getTrustLevelClass(fragment.primarySpeaker.trustLevel)"
              ></i>
            </span>
            <span v-if="fragment.confidence !== null && fragment.confidence !== undefined" class="ms-2">
              <i 
                :class="'fa-duotone ' + getConfidenceIcon(fragment.confidence)" 
                :style="{ color: getConfidenceColor(fragment.confidence) }"
                :title="'Confidence: ' + (fragment.confidence || '')"
              ></i>
            </span>
          </div>
        </div>
      </a>
    </li>
  </ul>
</template>

<script setup lang="ts">
import { 
  formatDate, 
  getSourceTypeIcon, 
  getIconClass, 
  getConfidenceIcon, 
  getConfidenceColor,
  getTrustLevelClass
} from '@/utils/helpers';
import type { FragmentDto, FragmentSearchResult } from '@/types/api-client';

// Props
interface Props {
  fragments: (FragmentDto | FragmentSearchResult)[];
  selectedId?: string | null;
}

const props = withDefaults(defineProps<Props>(), {
  fragments: () => [],
  selectedId: null
});

// Emits
interface Emits {
  (e: 'select', fragment: FragmentDto | FragmentSearchResult): void;
}

const emit = defineEmits<Emits>();

// Methods
function selectFragment(fragment: FragmentDto | FragmentSearchResult): void {
  emit('select', fragment);
  // Collapse left sidebar on mobile after selection
  (window as any).MedleySidebar?.collapseLeftSidebar();
}
</script>
