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
          <!-- Non-per-article-type templates -->
          <li v-for="template in nonPerArticleTypeTemplates" :key="`${template.type}`" class="list-item">
            <a href="#" 
               class="list-item-content"
               :class="{ active: isTemplateSelected(template) }"
               @click.prevent="selectTemplate(template)">
              <i class="list-item-icon bi bi-file-earmark-code"></i>
              <div class="list-item-body">
                <div class="list-item-title">
                  {{ template.name }}
                  <span v-if="!template.exists" class="badge bg-secondary ms-2" style="font-size: 0.65rem;">Not customized</span>
                </div>
                <div class="list-item-subtitle">{{ template.description }}</div>
              </div>
            </a>
          </li>

          <!-- Per-article-type templates (grouped) -->
          <template v-for="group in perArticleTypeGroups" :key="group.promptType">
            <li class="list-item list-item-group">
              <a href="#" 
                 class="list-item-content"
                 @click.prevent="toggleGroup(group.promptType)">
                <i class="list-item-icon bi" :class="group.expanded ? 'bi-chevron-down' : 'bi-chevron-right'"></i>
                <div class="list-item-body">
                  <div class="list-item-title">{{ group.name }}</div>
                  <div class="list-item-subtitle">{{ group.description }}</div>
                </div>
              </a>
            </li>
            <template v-if="group.expanded">
              <li v-for="template in group.templates" :key="`${template.type}-${template.articleTypeId}`" class="list-item list-item-child">
                <a href="#" 
                   class="list-item-content"
                   :class="{ active: isTemplateSelected(template) }"
                   @click.prevent="selectTemplate(template)">
                  <i class="list-item-icon" :class="getIconClass(getArticleTypeIcon(template.articleTypeId))"></i>
                  <div class="list-item-body">
                    <div class="list-item-title">
                      {{ template.articleTypeName }}
                      <span v-if="!template.exists" class="badge bg-secondary ms-2" style="font-size: 0.65rem;">Not customized</span>
                    </div>
                  </div>
                </a>
              </li>
            </template>
          </template>

          <!-- Per-fragment-category templates (grouped) -->
          <template v-for="group in perFragmentCategoryGroups" :key="group.promptType">
            <li class="list-item list-item-group">
              <a href="#" 
                 class="list-item-content"
                 @click.prevent="toggleGroup(group.promptType)">
                <i class="list-item-icon bi" :class="group.expanded ? 'bi-chevron-down' : 'bi-chevron-right'"></i>
                <div class="list-item-body">
                  <div class="list-item-title">{{ group.name }}</div>
                  <div class="list-item-subtitle">{{ group.description }}</div>
                </div>
              </a>
            </li>
            <template v-if="group.expanded">
              <li v-for="template in group.templates" :key="`${template.type}-${template.fragmentCategoryId}`" class="list-item list-item-child">
                <a href="#" 
                   class="list-item-content"
                   :class="{ active: isTemplateSelected(template) }"
                   @click.prevent="selectTemplate(template)">
                  <i class="list-item-icon" :class="getIconClass(getFragmentCategoryIcon(template.fragmentCategoryId))"></i>
                  <div class="list-item-body">
                    <div class="list-item-title">
                      {{ template.fragmentCategoryName }}
                      <span v-if="!template.exists" class="badge bg-secondary ms-2" style="font-size: 0.65rem;">Not customized</span>
                    </div>
                  </div>
                </a>
              </li>
            </template>
          </template>
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
          :key="`${selectedTemplate.type}-${selectedTemplate.articleTypeId || 'global'}`"
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
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { aiPromptsClient } from '@/utils/apiClients';
import { useSidebarState } from '@/composables/useSidebarState';
import { useArticleTypes } from '@/features/articles/composables/useArticleTypes';
import { useFragmentCategories } from '@/features/admin/composables/useFragmentCategories';
import { getIconClass } from '@/utils/helpers';
import type { AiPromptListDto, AiPromptDto, AiPromptType } from '@/types/api-client';

// Setup composables
const { leftSidebarVisible } = useSidebarState();
const router = useRouter();
const { typeIconMap, loadArticleTypes } = useArticleTypes();
const { categoryIconMap, loadFragmentCategories } = useFragmentCategories();

// Reactive state
const templates = ref<AiPromptListDto[]>([]);
const selectedTemplate = ref<AiPromptDto | null>(null);
const loading = ref<boolean>(false);
const error = ref<string | null>(null);
const editingContent = ref<string>('');
const isSaving = ref<boolean>(false);
const lastSaved = ref<Date | null>(null);
const userDisplayName = ref<string>(window.MedleyUser?.displayName || 'User');
const userIsAuthenticated = ref<boolean>(window.MedleyUser?.isAuthenticated || false);
const expandedGroups = ref<Set<number>>(new Set());

// Computed properties
const nonPerArticleTypeTemplates = computed(() => 
  templates.value.filter(t => !t.isPerArticleType && !t.isPerFragmentCategory)
);

interface PerArticleTypeGroup {
  promptType: number;
  name: string;
  description: string;
  expanded: boolean;
  templates: AiPromptListDto[];
}

const perArticleTypeGroups = computed<PerArticleTypeGroup[]>(() => {
  const perArticleTemplates = templates.value.filter(t => t.isPerArticleType);
  const groupMap = new Map<number, PerArticleTypeGroup>();

  for (const template of perArticleTemplates) {
    if (!groupMap.has(template.type)) {
      groupMap.set(template.type, {
        promptType: template.type,
        name: template.name,
        description: template.description || '',
        expanded: expandedGroups.value.has(template.type),
        templates: []
      });
    }
    groupMap.get(template.type)!.templates.push(template);
  }

  return Array.from(groupMap.values());
});

interface PerFragmentCategoryGroup {
  promptType: number;
  name: string;
  description: string;
  expanded: boolean;
  templates: AiPromptListDto[];
}

const perFragmentCategoryGroups = computed<PerFragmentCategoryGroup[]>(() => {
  const perFragmentTemplates = templates.value.filter(t => t.isPerFragmentCategory);
  const groupMap = new Map<number, PerFragmentCategoryGroup>();

  for (const template of perFragmentTemplates) {
    if (!groupMap.has(template.type)) {
      groupMap.set(template.type, {
        promptType: template.type,
        name: template.name,
        description: template.description || '',
        expanded: expandedGroups.value.has(template.type),
        templates: []
      });
    }
    groupMap.get(template.type)!.templates.push(template);
  }

  return Array.from(groupMap.values());
});

// Methods
const getArticleTypeIcon = (articleTypeId: string | null | undefined): string => {
  if (!articleTypeId) return 'bi-file-text';
  return typeIconMap.value[articleTypeId] || 'bi-file-text';
};

const getFragmentCategoryIcon = (fragmentCategoryId: string | null | undefined): string => {
  if (!fragmentCategoryId) return 'bi-puzzle';
  return categoryIconMap.value[fragmentCategoryId] || 'bi-puzzle';
};
const loadTemplates = async (): Promise<void> => {
  loading.value = true;
  error.value = null;
  try {
    templates.value = await aiPromptsClient.getAll();
  } catch (err: any) {
    error.value = 'Failed to load templates: ' + err.message;
    console.error('Error loading templates:', err);
  } finally {
    loading.value = false;
  }
};

const toggleGroup = (promptType: number): void => {
  if (expandedGroups.value.has(promptType)) {
    expandedGroups.value.delete(promptType);
  } else {
    expandedGroups.value.add(promptType);
  }
};

const isTemplateSelected = (template: AiPromptListDto): boolean => {
  if (!selectedTemplate.value) return false;
  return selectedTemplate.value.type === template.type && 
         selectedTemplate.value.articleTypeId === template.articleTypeId &&
         selectedTemplate.value.fragmentCategoryId === template.fragmentCategoryId;
};

const selectTemplate = async (template: AiPromptListDto): Promise<void> => {
  try {
    const fullTemplate = await aiPromptsClient.get(
      template.type as AiPromptType,
      template.articleTypeId || undefined,
      template.fragmentCategoryId || undefined
    );
    
    selectedTemplate.value = fullTemplate;
    editingContent.value = fullTemplate.content || '';
    lastSaved.value = fullTemplate.lastModifiedAt ? new Date(fullTemplate.lastModifiedAt) : null;
    
    // Update URL
    const query: any = { type: template.type };
    if (template.articleTypeId) {
      query.articleTypeId = template.articleTypeId;
    }
    if (template.fragmentCategoryId) {
      query.fragmentCategoryId = template.fragmentCategoryId;
    }
    await router.push({ query });

    // Expand the group if it's a per-article-type or per-fragment-category template
    if (template.isPerArticleType || template.isPerFragmentCategory) {
      expandedGroups.value.add(template.type);
    }
  } catch (err: any) {
    console.error('Error loading template:', err);
    error.value = 'Failed to load template: ' + err.message;
  }
};

const saveTemplate = async (): Promise<void> => {
  if (!selectedTemplate.value || isSaving.value) return;

  isSaving.value = true;
  try {
    const updated = await aiPromptsClient.createOrUpdate(
      selectedTemplate.value.type,
      { content: editingContent.value },
      selectedTemplate.value.articleTypeId || undefined,
      selectedTemplate.value.fragmentCategoryId || undefined
    );

    selectedTemplate.value = updated;
    lastSaved.value = new Date();

    // Update the template in the list to mark it as existing
    const templateInList = templates.value.find(t => 
      t.type === updated.type && 
      t.articleTypeId === updated.articleTypeId &&
      t.fragmentCategoryId === updated.fragmentCategoryId
    );
    if (templateInList) {
      templateInList.exists = true;
      templateInList.id = updated.id;
      templateInList.lastModifiedAt = updated.lastModifiedAt;
    }
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
  await loadArticleTypes();
  await loadFragmentCategories();
  await loadTemplates();

  const urlParams = new URLSearchParams(window.location.search);
  const typeParam = urlParams.get('type');
  const articleTypeIdParam = urlParams.get('articleTypeId');
  const fragmentCategoryIdParam = urlParams.get('fragmentCategoryId');
  
  if (typeParam) {
    const type = parseInt(typeParam);
    const template = templates.value.find(t => 
      t.type === type && 
      (articleTypeIdParam ? t.articleTypeId === articleTypeIdParam : !t.articleTypeId) &&
      (fragmentCategoryIdParam ? t.fragmentCategoryId === fragmentCategoryIdParam : !t.fragmentCategoryId)
    );
    if (template) {
      await selectTemplate(template);
    }
  } else if (templates.value.length > 0) {
    // Select first non-per-article-type template by default
    const firstTemplate = nonPerArticleTypeTemplates.value[0] || templates.value[0];
    if (firstTemplate) {
      await selectTemplate(firstTemplate);
    }
  }

  window.addEventListener('popstate', async () => {
    const urlParams = new URLSearchParams(window.location.search);
    const typeParam = urlParams.get('type');
    const articleTypeIdParam = urlParams.get('articleTypeId');
    const fragmentCategoryIdParam = urlParams.get('fragmentCategoryId');
    
    if (typeParam) {
      const type = parseInt(typeParam);
      const template = templates.value.find(t => 
        t.type === type && 
        (articleTypeIdParam ? t.articleTypeId === articleTypeIdParam : !t.articleTypeId) &&
        (fragmentCategoryIdParam ? t.fragmentCategoryId === fragmentCategoryIdParam : !t.fragmentCategoryId)
      );
      if (template && !isTemplateSelected(template)) {
        await selectTemplate(template);
      }
    }
  });
});
</script>

<style scoped>

.template-save-indicator {
  padding: 0.5rem 1.5rem;
  font-size: 0.75rem;
  border-top: 1px solid var(--bs-border-color);
  background-color: var(--bs-body-bg);
}

.list-item-group .list-item-content {
  font-weight: 500;
}

.list-item-child {
  padding-left: 1rem;
}

.list-item-child .list-item-content {
  padding-left: 0.5rem;
}
</style>
