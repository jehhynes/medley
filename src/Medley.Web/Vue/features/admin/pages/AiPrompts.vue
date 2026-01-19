<template>
    <vertical-menu 
      :display-name="userDisplayName"
      :is-authenticated="userIsAuthenticated"
    />

    <!-- Left Sidebar (Template List) -->
    <div class="sidebar left-sidebar" :class="{ 'show': leftSidebarVisible }">
      <div class="sidebar-header">
        <h6 class="sidebar-title sidebar-breadcrumb-title">
          <a href="/Admin/Settings">Settings</a>
          <i class="bi bi-chevron-right"></i>
          <span>AI Prompts</span>
        </h6>
      </div>
      <div class="sidebar-content">
        <div v-if="loading" class="loading-spinner">
          <div class="spinner-border spinner-border-sm" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
        </div>
        <div v-else-if="error" class="alert alert-danger" v-cloak>
          {{ error }}
        </div>
        <ul v-else class="list-view" v-cloak>
          <li v-for="template in templates" :key="template.id" class="list-item">
            <a href="#" 
               class="list-item-content"
               :class="{ active: selectedTemplateId === template.id }"
               @click.prevent="selectTemplate(template)">
              <i class="list-item-icon bi bi-file-earmark-code"></i>
              <div class="list-item-body">
                <div class="list-item-title">{{ template.name }}</div>
                <div class="list-item-subtitle">{{ template.description || template.typeName }}</div>
              </div>
            </a>
          </li>
        </ul>
      </div>
    </div>

    <!-- Main Content -->
    <div class="main-content" :class="{ 'd-flex flex-column': selectedTemplate }" :style="selectedTemplate ? 'padding: 0;' : ''">
      <div v-if="!selectedTemplate" class="empty-state" v-cloak>
        <div class="empty-state-icon">
          <i class="bi bi-file-earmark-code"></i>
        </div>
        <div class="empty-state-title">No Prompt Selected</div>
        <div class="empty-state-text">Select a prompt template from the sidebar to edit it</div>
      </div>

      <template v-else v-cloak>
        <!-- Editor -->
        <tiptap-editor 
          v-model="editingContent"
          :key="selectedTemplateId"
          :is-saving="isSaving"
          @save="saveTemplate"
          class="flex-grow-1"
          placeholder="Enter the prompt template content..." />

        <!-- Save indicator -->
        <div v-if="lastSaved" class="template-save-indicator text-muted">
          <i class="bi bi-check-circle me-1"></i>
          Last saved: {{ formatTime(lastSaved) }}
        </div>
      </template>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { api } from '@/utils/api';
import { useSidebarState } from '@/composables/useSidebarState';
import type { Template } from '@/types/api-client';

// Setup composables
const { leftSidebarVisible } = useSidebarState();
const router = useRouter();

// Reactive state
const templates = ref<Template[]>([]);
const selectedTemplateId = ref<string | null>(null);
const selectedTemplate = ref<Template | null>(null);
const loading = ref<boolean>(false);
const error = ref<string | null>(null);
const editingContent = ref<string>('');
const isSaving = ref<boolean>(false);
const lastSaved = ref<Date | null>(null);
const userDisplayName = ref<string>(window.MedleyUser?.displayName || 'User');
const userIsAuthenticated = ref<boolean>(window.MedleyUser?.isAuthenticated || false);

// Methods
const loadTemplates = async (): Promise<void> => {
  loading.value = true;
  error.value = null;
  try {
    const data = await api.get('/api/templates');
    templates.value = data as Template[];
  } catch (err: any) {
    error.value = 'Failed to load templates: ' + err.message;
    console.error('Error loading templates:', err);
  } finally {
    loading.value = false;
  }
};

const selectTemplate = async (template: Template): Promise<void> => {
  try {
    const fullTemplate = await api.get(`/api/templates/${template.id}`) as Template;
    
    selectedTemplate.value = fullTemplate;
    selectedTemplateId.value = template.id;
    editingContent.value = fullTemplate.content || '';
    lastSaved.value = fullTemplate.lastModifiedAt ? new Date(fullTemplate.lastModifiedAt) : null;
    
    await router.push({ query: { id: template.id } });
  } catch (err: any) {
    console.error('Error loading template:', err);
    error.value = 'Failed to load template: ' + err.message;
  }
};

const saveTemplate = async (): Promise<void> => {
  if (!selectedTemplate.value || isSaving.value) return;

  isSaving.value = true;
  try {
    const updated = await api.put(`/api/templates/${selectedTemplate.value.id}`, {
      content: editingContent.value
    }) as Template;

    selectedTemplate.value = updated;
    lastSaved.value = new Date();
  } catch (err: any) {
    console.error('Error saving template:', err);
    alert('Failed to save template: ' + err.message);
  } finally {
    isSaving.value = false;
  }
};

const formatTime = (date: Date | null): string => {
  if (!date) return '';
  return new Date(date).toLocaleTimeString();
};

// Lifecycle hooks
onMounted(async () => {
  await loadTemplates();

  const urlParams = new URLSearchParams(window.location.search);
  const templateIdFromUrl = urlParams.get('id');
  if (templateIdFromUrl) {
    const template = templates.value.find(t => t.id === templateIdFromUrl);
    if (template) {
      await selectTemplate(template);
    }
  } else if (templates.value.length > 0) {
    await selectTemplate(templates.value[0]);
  }

  window.addEventListener('popstate', async () => {
    const urlParams = new URLSearchParams(window.location.search);
    const templateId = urlParams.get('id');
    if (templateId && templateId !== selectedTemplateId.value) {
      const template = templates.value.find(t => t.id === templateId);
      if (template) {
        await selectTemplate(template);
      }
    }
  });
});
</script>

<style scoped>
.sidebar-breadcrumb-title {
  display: flex;
  align-items: center;
  gap: 0.375rem;
}

.sidebar-breadcrumb-title a {
  color: var(--bs-secondary-color);
  text-decoration: none;
  font-weight: 500;
}

.sidebar-breadcrumb-title a:hover {
  color: var(--bs-primary);
}

.sidebar-breadcrumb-title i {
  font-size: 0.6rem;
  color: var(--bs-secondary-color);
}

.sidebar-breadcrumb-title span {
  color: var(--bs-body-color);
}

.template-save-indicator {
  padding: 0.5rem 1.5rem;
  font-size: 0.75rem;
  border-top: 1px solid var(--bs-border-color);
  background-color: var(--bs-body-bg);
}
</style>
