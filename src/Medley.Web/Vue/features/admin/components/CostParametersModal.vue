<template>
  <Teleport to="body">
    <div 
      class="modal fade" 
      :class="{ show: isVisible }" 
      :style="{ display: isVisible ? 'block' : 'none' }"
      tabindex="-1"
      @click.self="handleCancel"
    >
      <div class="modal-dialog modal-lg modal-dialog-scrollable">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">
              <i class="bi bi-currency-dollar me-2"></i>Configure Cost Parameters
            </h5>
            <button type="button" class="btn-close" @click="handleCancel"></button>
          </div>
          
          <div class="modal-body">
            <div v-if="loading" class="text-center py-4">
              <div class="spinner-border" role="status">
                <span class="visually-hidden">Loading...</span>
              </div>
            </div>

            <div v-else>
              <p class="mb-4 text-muted">
                Configure cost per million tokens for each AI model. Only fields relevant to each model are shown.
              </p>

              <div v-if="modelParameters.length === 0" class="alert alert-info">
                No models found with token usage data.
              </div>

              <div v-for="param in modelParameters" :key="param.modelName" class="model-parameter-card mb-3">
                <div class="model-name">{{ param.modelName }}</div>
                
                <div class="row g-3">
                  <div v-if="param.hasInputTokens" class="col-md-4">
                    <label class="form-label">Input Cost (per 1M tokens)</label>
                    <div class="input-group input-group-sm">
                      <span class="input-group-text">$</span>
                      <input 
                        type="number" 
                        class="form-control" 
                        v-model.number="param.inputCostPerMillion"
                        step="0.01"
                        min="0"
                        placeholder="0.00"
                      />
                    </div>
                  </div>

                  <div v-if="param.hasOutputTokens" class="col-md-4">
                    <label class="form-label">Output Cost (per 1M tokens)</label>
                    <div class="input-group input-group-sm">
                      <span class="input-group-text">$</span>
                      <input 
                        type="number" 
                        class="form-control" 
                        v-model.number="param.outputCostPerMillion"
                        step="0.01"
                        min="0"
                        placeholder="0.00"
                      />
                    </div>
                  </div>

                  <div v-if="param.hasEmbeddingTokens" class="col-md-4">
                    <label class="form-label">Embedding Cost (per 1M tokens)</label>
                    <div class="input-group input-group-sm">
                      <span class="input-group-text">$</span>
                      <input 
                        type="number" 
                        class="form-control" 
                        v-model.number="param.embeddingCostPerMillion"
                        step="0.01"
                        min="0"
                        placeholder="0.00"
                      />
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="handleCancel">Cancel</button>
            <button type="button" class="btn btn-primary" @click="handleSave" :disabled="saving">
              <span v-if="saving" class="spinner-border spinner-border-sm me-2"></span>
              Save Parameters
            </button>
          </div>
        </div>
      </div>
    </div>
    
    <div 
      v-if="isVisible" 
      class="modal-backdrop fade show"
      @click="handleCancel"
    ></div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { tokenUsageClient } from '@/utils/apiClients';
import type { ModelInfo, ModelCostParameter, SaveCostParameterRequest } from '@/types/api-client';

const props = defineProps<{
  visible: boolean;
}>();

const emit = defineEmits<{
  'update:visible': [value: boolean];
  'saved': [];
}>();

const isVisible = ref(props.visible);
const loading = ref(false);
const saving = ref(false);
const modelParameters = ref<Array<ModelInfo & Partial<ModelCostParameter>>>([]);

watch(() => props.visible, (newVal) => {
  isVisible.value = newVal;
  if (newVal) {
    loadData();
  }
});

watch(isVisible, (newVal) => {
  emit('update:visible', newVal);
});

const loadData = async () => {
  loading.value = true;
  try {
    // Load models and existing cost parameters
    const [models, costParams] = await Promise.all([
      tokenUsageClient.getModels(),
      tokenUsageClient.getCostParameters()
    ]);

    // Merge model info with existing cost parameters
    modelParameters.value = models.map(model => {
      const existingParam = costParams.find(p => p.modelName === model.modelName);
      return {
        ...model,
        id: existingParam?.id,
        inputCostPerMillion: existingParam?.inputCostPerMillion ?? undefined,
        outputCostPerMillion: existingParam?.outputCostPerMillion ?? undefined,
        embeddingCostPerMillion: existingParam?.embeddingCostPerMillion ?? undefined
      };
    });
  } catch (error) {
    console.error('Failed to load cost parameters:', error);
    alert('Failed to load cost parameters');
  } finally {
    loading.value = false;
  }
};

const handleSave = async () => {
  saving.value = true;
  try {
    // Save all parameters
    const savePromises = modelParameters.value.map(param => {
      const request: SaveCostParameterRequest = {
        modelName: param.modelName!,
        inputCostPerMillion: param.hasInputTokens ? (param.inputCostPerMillion ?? null) : null,
        outputCostPerMillion: param.hasOutputTokens ? (param.outputCostPerMillion ?? null) : null,
        embeddingCostPerMillion: param.hasEmbeddingTokens ? (param.embeddingCostPerMillion ?? null) : null
      };
      return tokenUsageClient.saveCostParameter(request);
    });

    await Promise.all(savePromises);
    
    emit('saved');
    isVisible.value = false;
  } catch (error) {
    console.error('Failed to save cost parameters:', error);
    alert('Failed to save cost parameters');
  } finally {
    saving.value = false;
  }
};

const handleCancel = () => {
  isVisible.value = false;
};
</script>

<style scoped>
.model-parameter-card {
  background: var(--bs-body-bg);
  border: 1px solid var(--bs-border-color);
  border-radius: 8px;
  padding: 1rem;
}

.model-name {
  font-weight: 600;
  font-size: 0.95rem;
  margin-bottom: 1rem;
  color: var(--bs-body-color);
  font-family: 'Courier New', monospace;
}

.form-label {
  font-size: 0.85rem;
  font-weight: 500;
  margin-bottom: 0.25rem;
}

.input-group-text {
  background-color: var(--bs-secondary-bg);
  border-color: var(--bs-border-color);
}
</style>
