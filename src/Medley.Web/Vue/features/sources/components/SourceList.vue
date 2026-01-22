<template>
  <ul class="list-view">
    <li v-for="source in sources" :key="source.id" class="list-item">
      <a href="#" 
         class="list-item-content" 
         :class="{ active: selectedId === source.id }"
         @click.prevent="selectSource(source)">
        <i class="list-item-icon bi" :class="getSourceIcon(source.type)"></i>
        <div class="list-item-body">
          <div class="list-item-title">{{ source.name || 'Untitled' }}</div>
          <div class="list-item-subtitle">
            <span>{{ formatDate(source.date) }} • {{ source.type }}</span>
            <span v-if="source.primarySpeakerName">
              •
              {{ source.primarySpeakerName }}
              <i 
                v-if="source.primarySpeakerTrustLevel" 
                class="bi bi-shield-check" 
                :class="getTrustLevelClass(source.primarySpeakerTrustLevel)"
              ></i>
            </span>
          </div>
        </div>
        <span class="list-item-status ms-auto">
          <span v-if="source.extractionStatus === 'InProgress'" 
                class="spinner-border spinner-border-sm text-secondary" 
                role="status"
                style="width: 1em; height: 1em;">
            <span class="visually-hidden">Extracting...</span>
          </span>
          <i v-else-if="source.extractionStatus === 'Completed'" 
             class="bi bi-check-lg text-success"></i>
          <i v-else-if="source.extractionStatus === 'Failed'" 
             class="bi bi-x-square text-danger"></i>
        </span>
      </a>
    </li>
  </ul>
</template>

<script setup lang="ts">
import { 
  formatDate, 
  getSourceTypeIcon 
} from '@/utils/helpers';
import type { SourceDto } from '@/types/api-client';

// Props
interface Props {
  sources: SourceDto[];
  selectedId?: string | null;
}

const props = withDefaults(defineProps<Props>(), {
  sources: () => [],
  selectedId: null
});

// Emits
interface Emits {
  (e: 'select', source: SourceDto): void;
}

const emit = defineEmits<Emits>();

// Methods
function selectSource(source: SourceDto): void {
  emit('select', source);
  // Collapse left sidebar on mobile after selection
  (window as any).MedleySidebar?.collapseLeftSidebar();
}

function getTrustLevelClass(trustLevel: string | null | undefined): string {
  if (!trustLevel) return '';
  switch (trustLevel) {
    case 'High': return 'text-success';
    case 'Medium': return 'text-warning';
    case 'Low': return 'text-danger';
    default: return '';
  }
}

// Expose utility functions to template
const getSourceIcon = getSourceTypeIcon;
</script>
