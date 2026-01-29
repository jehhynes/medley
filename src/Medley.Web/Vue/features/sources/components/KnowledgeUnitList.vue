<template>
  <ul class="list-view">
    <li v-for="knowledgeUnit in knowledgeUnits" :key="knowledgeUnit.id" class="list-item">
      <a href="#" 
         class="list-item-content" 
         :class="{ active: selectedId === knowledgeUnit.id }"
         @click.prevent="selectKnowledgeUnit(knowledgeUnit)">
        <i class="list-item-icon" :class="getIconClass(knowledgeUnit.categoryIcon, 'fal fa-atom')"></i>
        <div class="list-item-body">
          <div class="list-item-title">{{ knowledgeUnit.title || 'Untitled Knowledge Unit' }}</div>
          <div class="list-item-subtitle">
            {{ formatDate(knowledgeUnit.updatedAt) }} • {{ knowledgeUnit.fragmentCount }} fragment{{ knowledgeUnit.fragmentCount !== 1 ? 's' : '' }}
            <span v-if="knowledgeUnit.similarity !== undefined" class="badge bg-info ms-2" :title="'Similarity: ' + (knowledgeUnit.similarity * 100).toFixed(1) + '%'">
              {{ (knowledgeUnit.similarity * 100).toFixed(0) }}%
            </span>
            <span v-if="knowledgeUnit.confidence" class="ms-2">
              <i 
                :class="'fa-duotone ' + getConfidenceIcon(knowledgeUnit.confidence)" 
                :style="{ color: getConfidenceColor(knowledgeUnit.confidence) }"
                :title="'Confidence: ' + knowledgeUnit.confidence"
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
  getIconClass, 
  getConfidenceIcon, 
  getConfidenceColor
} from '@/utils/helpers';
import type { KnowledgeUnitDto, KnowledgeUnitSearchResult } from '@/types/api-client';

// Props
interface Props {
  knowledgeUnits: (KnowledgeUnitDto | KnowledgeUnitSearchResult)[];
  selectedId?: string | null;
}

const props = withDefaults(defineProps<Props>(), {
  knowledgeUnits: () => [],
  selectedId: null
});

// Emits
interface Emits {
  (e: 'select', knowledgeUnit: KnowledgeUnitDto | KnowledgeUnitSearchResult): void;
}

const emit = defineEmits<Emits>();

// Methods
function selectKnowledgeUnit(knowledgeUnit: KnowledgeUnitDto | KnowledgeUnitSearchResult): void {
  emit('select', knowledgeUnit);
  // Collapse left sidebar on mobile after selection
  (window as any).MedleySidebar?.collapseLeftSidebar();
}
</script>
