<template>
  <div class="knowledge-units-viewer h-100 d-flex flex-column">
    <div class="knowledge-units-content flex-grow-1 overflow-auto">
      <div v-if="loading" class="d-flex justify-content-center align-items-center h-100">
        <div class="spinner-border" role="status">
          <span class="visually-hidden">Loading knowledge units...</span>
        </div>
      </div>
      
      <div v-else-if="error" class="alert alert-danger m-3">
        {{ error }}
      </div>
      
      <div v-else-if="knowledgeUnits.length === 0" class="empty-state">
        <div class="empty-state-icon">
          <i class="bi bi-puzzle"></i>
        </div>
        <div class="empty-state-title">No Knowledge Units</div>
        <div class="empty-state-text">No knowledge units are linked to this article yet</div>
      </div>
      
      <knowledge-unit-list
        v-else
        :knowledge-units="knowledgeUnits"
        :selected-id="selectedKnowledgeUnitId"
        @select="handleKnowledgeUnitSelect"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import { apiClients } from '@/utils/apiClients';
import type { KnowledgeUnitDto } from '@/types/api-client';
import KnowledgeUnitList from '../../sources/components/KnowledgeUnitList.vue';

// Props
interface Props {
  articleId: string | null;
}

const props = defineProps<Props>();

// Emits
interface Emits {
  (e: 'open-knowledge-unit', knowledgeUnitId: string): void;
}

const emit = defineEmits<Emits>();

// State
const knowledgeUnits = ref<KnowledgeUnitDto[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);
const selectedKnowledgeUnitId = ref<string | null>(null);

// Methods
const loadKnowledgeUnits = async () => {
  if (!props.articleId) {
    knowledgeUnits.value = [];
    return;
  }

  loading.value = true;
  error.value = null;

  try {
    knowledgeUnits.value = await apiClients.knowledgeUnits.getByArticleId(props.articleId);
  } catch (err: any) {
    console.error('Error loading knowledge units:', err);
    error.value = 'Failed to load knowledge units';
  } finally {
    loading.value = false;
  }
};

const handleKnowledgeUnitSelect = (knowledgeUnit: KnowledgeUnitDto) => {
  selectedKnowledgeUnitId.value = knowledgeUnit.id ?? null;
  if (knowledgeUnit.id) {
    emit('open-knowledge-unit', knowledgeUnit.id);
  }
};

// Watch for article changes
watch(() => props.articleId, () => {
  selectedKnowledgeUnitId.value = null;
  loadKnowledgeUnits();
}, { immediate: true });

// Expose methods for parent component
defineExpose({
  loadKnowledgeUnits
});
</script>
