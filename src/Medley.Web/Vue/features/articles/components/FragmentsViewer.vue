<template>
  <div class="fragments-viewer h-100 d-flex flex-column">
    <div class="fragments-content flex-grow-1 overflow-auto">
      <div v-if="loading" class="d-flex justify-content-center align-items-center h-100">
        <div class="spinner-border" role="status">
          <span class="visually-hidden">Loading fragments...</span>
        </div>
      </div>
      
      <div v-else-if="error" class="alert alert-danger m-3">
        {{ error }}
      </div>
      
      <div v-else-if="fragments.length === 0" class="empty-state">
        <div class="empty-state-icon">
          <i class="bi bi-puzzle"></i>
        </div>
        <div class="empty-state-title">No Fragments</div>
        <div class="empty-state-text">No fragments are linked to this article yet</div>
      </div>
      
      <fragment-list
        v-else
        :fragments="fragments"
        :selected-id="selectedFragmentId"
        @select="handleFragmentSelect"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import { apiClients } from '@/utils/apiClients';
import type { FragmentDto } from '@/types/api-client';
import FragmentList from '../../sources/components/FragmentList.vue';

// Props
interface Props {
  articleId: string | null;
}

const props = defineProps<Props>();

// Emits
interface Emits {
  (e: 'open-fragment', fragmentId: string): void;
}

const emit = defineEmits<Emits>();

// State
const fragments = ref<FragmentDto[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);
const selectedFragmentId = ref<string | null>(null);

// Methods
const loadFragments = async () => {
  if (!props.articleId) {
    fragments.value = [];
    return;
  }

  loading.value = true;
  error.value = null;

  try {
    fragments.value = await apiClients.fragments.getByArticleId(props.articleId);
  } catch (err: any) {
    console.error('Error loading fragments:', err);
    error.value = 'Failed to load fragments';
  } finally {
    loading.value = false;
  }
};

const handleFragmentSelect = (fragment: FragmentDto) => {
  selectedFragmentId.value = fragment.id ?? null;
  if (fragment.id) {
    emit('open-fragment', fragment.id);
  }
};

// Watch for article changes
watch(() => props.articleId, () => {
  selectedFragmentId.value = null;
  loadFragments();
}, { immediate: true });

// Expose methods for parent component
defineExpose({
  loadFragments
});
</script>