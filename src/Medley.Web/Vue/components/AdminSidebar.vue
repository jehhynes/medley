<template>
  <div class="sidebar left-sidebar" :class="{ 'show': leftSidebarVisible }">
    <div class="sidebar-header">
      <h6 class="sidebar-title sidebar-breadcrumb-title">
        <a href="/Admin/Settings">Settings</a>
        <template v-if="breadcrumbTitle">
          <i class="bi bi-chevron-right"></i>
          <span>{{ breadcrumbTitle }}</span>
        </template>
      </h6>
    </div>
    <div class="sidebar-content">
      <ul class="list-view">
        <li class="list-item">
          <a href="/Admin/Settings" class="list-item-content" :class="{ active: currentPage === 'Settings' }">
            <i class="list-item-icon bi bi-gear"></i>
            <div class="list-item-body">
              <div class="list-item-title">General</div>
            </div>
          </a>
        </li>
        <li class="list-item">
          <a href="/Admin/ReviewSettings" class="list-item-content" :class="{ active: currentPage === 'ReviewSettings' }">
            <i class="list-item-icon bi bi-list-check"></i>
            <div class="list-item-body">
              <div class="list-item-title">Article Review Settings</div>
            </div>
          </a>
        </li>
        <li class="list-item">
          <a href="/Admin/AiPrompts" class="list-item-content" :class="{ active: currentPage === 'AiPrompts' }">
            <i class="list-item-icon bi bi-robot"></i>
            <div class="list-item-body">
              <div class="list-item-title">AI Prompts</div>
            </div>
          </a>
        </li>
        <li class="list-item">
          <a href="/Admin/TagTypes" class="list-item-content" :class="{ active: currentPage === 'TagTypes' }">
            <i class="list-item-icon bi bi-tags"></i>
            <div class="list-item-body">
              <div class="list-item-title">Tags</div>
            </div>
          </a>
        </li>
        <li class="list-item">
          <a href="/Admin/ArticleTypes" class="list-item-content" :class="{ active: currentPage === 'ArticleTypes' }">
            <i class="list-item-icon bi bi-file-text"></i>
            <div class="list-item-body">
              <div class="list-item-title">Article Types</div>
            </div>
          </a>
        </li>
        <li class="list-item">
          <a href="/Admin/KnowledgeCategories" class="list-item-content" :class="{ active: currentPage === 'KnowledgeCategories' }">
            <i class="list-item-icon fa-light fa-atom"></i>
            <div class="list-item-body">
              <div class="list-item-title">Knowledge Categories</div>
            </div>
          </a>
        </li>
        <li class="list-item">
          <a href="/Admin/Speakers" class="list-item-content" :class="{ active: currentPage === 'Speakers' }">
            <i class="list-item-icon bi bi-person"></i>
            <div class="list-item-body">
              <div class="list-item-title">Speakers</div>
            </div>
          </a>
        </li>
        <li class="list-item">
          <a href="/Integrations/Manage" class="list-item-content" :class="{ active: currentPage === 'Integrations' }">
            <i class="list-item-icon bi bi-plug"></i>
            <div class="list-item-body">
              <div class="list-item-title">Integrations</div>
            </div>
          </a>
        </li>
        <li class="list-item" v-if="isAdmin">
          <a href="/Admin/Users" class="list-item-content" :class="{ active: currentPage === 'Users' }">
            <i class="list-item-icon bi bi-people"></i>
            <div class="list-item-body">
              <div class="list-item-title">Users</div>
            </div>
          </a>
        </li>
      </ul>
    </div>

    <template v-if="isAdmin">
      <div class="sidebar-header">
        <h6 class="sidebar-title">Utilities</h6>
      </div>
      <div class="sidebar-content">
        <ul class="list-view">
          <li class="list-item">
            <a href="/Admin/KnowledgeBuilderImport" class="list-item-content" :class="{ active: currentPage === 'KnowledgeBuilderImport' }">
              <i class="list-item-icon bi bi-file-earmark-arrow-up"></i>
              <div class="list-item-body">
                <div class="list-item-title">Knowledge Builder Import</div>
              </div>
            </a>
          </li>
          <li class="list-item">
            <a href="/Admin/SourceImport" class="list-item-content" :class="{ active: currentPage === 'SourceImport' }">
              <i class="list-item-icon bi bi-file-earmark-arrow-up"></i>
              <div class="list-item-body">
                <div class="list-item-title">Collector Import</div>
              </div>
            </a>
          </li>
          <li class="list-item">
            <a href="/Admin/TokenUsage" class="list-item-content" :class="{ active: currentPage === 'TokenUsage' }">
              <i class="list-item-icon bi bi-graph-up"></i>
              <div class="list-item-body">
                <div class="list-item-title">Token Usage</div>
              </div>
            </a>
          </li>
        </ul>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { useSidebarState } from '@/composables/useSidebarState';

interface Props {
  breadcrumbTitle?: string;
  isAdmin?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  breadcrumbTitle: '',
  isAdmin: false
});

const route = useRoute();
const { leftSidebarVisible } = useSidebarState();

const currentPage = computed<string>(() => {
  const path = route.path;
  
  // Extract controller name from path
  if (path.includes('/Admin/Settings')) return 'Settings';
  if (path.includes('/Admin/ReviewSettings')) return 'ReviewSettings';
  if (path.includes('/Admin/AiPrompts')) return 'AiPrompts';
  if (path.includes('/Admin/TagTypes')) return 'TagTypes';
  if (path.includes('/Admin/ArticleTypes')) return 'ArticleTypes';
  if (path.includes('/Admin/KnowledgeCategories')) return 'KnowledgeCategories';
  if (path.includes('/Admin/Speakers')) return 'Speakers';
  if (path.includes('/Integrations')) return 'Integrations';
  if (path.includes('/Admin/Users')) return 'Users';
  if (path.includes('/Admin/KnowledgeBuilderImport')) return 'KnowledgeBuilderImport';
  if (path.includes('/Admin/SourceImport')) return 'SourceImport';
  if (path.includes('/Admin/TokenUsage')) return 'TokenUsage';
  
  return '';
});
</script>
