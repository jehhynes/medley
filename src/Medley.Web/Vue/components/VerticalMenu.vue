<template>
  <div class="vertical-menu" :class="{ 'show': leftSidebarVisible }">
    <template v-if="isAuthenticated">
      <router-link to="/" 
         :class="['vertical-menu-item', { active: currentPage === 'Home' }]" 
         title="Home">
        <i class="bi bi-house"></i>
        <span class="vertical-menu-item-label">Home</span>
      </router-link>
      <router-link to="/Articles"
         :class="['vertical-menu-item', { active: currentPage === 'Articles' }]"
         title="Articles">
        <i class="bi bi-file-text"></i>
        <span class="vertical-menu-item-label">Articles</span>
      </router-link>
      <router-link to="/KnowledgeUnits" 
         :class="['vertical-menu-item', { active: currentPage === 'KnowledgeUnits' }]" 
         title="Knowledge">
        <i class="fal fa-atom"></i>
        <span class="vertical-menu-item-label">Knowledge</span>
      </router-link>
      <router-link to="/Sources" 
         :class="['vertical-menu-item', { active: currentPage === 'Sources' }]" 
         title="Sources">
        <i class="bi bi-camera-video"></i>
        <span class="vertical-menu-item-label">Sources</span>
      </router-link>
      <router-link to="/Fragments" 
         :class="['vertical-menu-item', { active: currentPage === 'Fragments' }]" 
         title="Fragments">
        <i class="bi bi-puzzle"></i>
        <span class="vertical-menu-item-label">Fragments</span>
      </router-link>
      <div style="flex: 1;"></div>
      <a href="/Admin/Settings" 
         :class="['vertical-menu-item', { active: isAdminPage }]" 
         title="Admin">
        <i class="bi bi-gear"></i>
        <span class="vertical-menu-item-label">Admin</span>
      </a>
      <div class="dropdown-container">
        <button class="vertical-menu-item" type="button" @click="toggleDropdown($event, 'user-menu')" title="User">
          <i class="bi bi-person-circle"></i>
        </button>
        <ul v-if="isDropdownOpen('user-menu')" class="dropdown-menu dropdown-menu-end show" :class="getPositionClasses()">
          <li><h6 class="dropdown-header">{{ displayName }}</h6></li>
          <li><hr class="dropdown-divider"></li>
          <li>
            <a href="/Account/ChangePassword" class="dropdown-item">
              <i class="bi bi-key me-2"></i>Change Password
            </a>
          </li>
          <li><hr class="dropdown-divider"></li>
          <li>
            <form action="/Auth/Logout" method="post">
              <button type="submit" class="dropdown-item">
                <i class="bi bi-box-arrow-right me-2"></i>Logout
              </button>
            </form>
          </li>
        </ul>
      </div>
    </template>
    <template v-else>
      <a href="/Auth/Login" 
         :class="['vertical-menu-item', { active: currentPage === 'Auth' }]" 
         title="Login">
        <i class="bi bi-box-arrow-in-right"></i>
        <span class="vertical-menu-item-label">Login</span>
      </a>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { useSidebarState } from '@/composables/useSidebarState';
import { useDropDown } from '@/composables/useDropDown';

// Props
interface Props {
  displayName?: string;
  isAuthenticated?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  displayName: 'User',
  isAuthenticated: false
});

// Composables
const { leftSidebarVisible } = useSidebarState();
const route = useRoute();
const { toggleDropdown, isDropdownOpen, getPositionClasses } = useDropDown();

// Computed
const currentPage = computed<string | null>(() => {
  // Check if it's an admin page first - don't return 'Home' for admin pages
  if (isAdminPage.value) {
    return null;
  }
  
  const path = route.path.toLowerCase();
  const segments = path.split('/').filter(s => s);
  const firstSegment = segments[0] || 'home';
  
  switch(firstSegment) {
    case 'articles': return 'Articles';
    case 'knowledgeunits': return 'KnowledgeUnits';
    case 'sources': return 'Sources';
    case 'fragments': return 'Fragments';
    case 'auth': return 'Auth';
    case 'account': return 'Account';
    case 'home':
    default: return 'Home';
  }
});

const isAdminPage = computed<boolean>(() => {
  const path = route.path.toLowerCase();
  const segments = path.split('/').filter(s => s);
  const firstSegment = segments[0] || '';
  return firstSegment === 'admin' || firstSegment === 'integrations';
});
</script>
