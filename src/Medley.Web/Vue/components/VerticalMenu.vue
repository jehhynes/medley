<template>
  <div class="vertical-menu" :class="{ 'show': isOpen }">
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
      <div class="dropdown">
        <button class="vertical-menu-item" type="button" data-bs-toggle="dropdown" aria-expanded="false" title="User">
          <i class="bi bi-person-circle"></i>
        </button>
        <ul class="dropdown-menu dropdown-menu-end">
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

<script>
export default {
  name: 'VerticalMenu',
  props: {
    // User display name (from server)
    displayName: {
      type: String,
      default: 'User'
    },
    // Whether user is authenticated (from server)
    isAuthenticated: {
      type: Boolean,
      default: false
    },
    // Whether to show menu on mobile initially (when no item selected)
    isOpen: {
      type: Boolean,
      default: false
    }
  },
  computed: {
    currentPage() {
      const path = this.$route.path.toLowerCase();
      const segments = path.split('/').filter(s => s);
      const firstSegment = segments[0] || 'home';
      
      switch(firstSegment) {
        case 'articles': return 'Articles';
        case 'sources': return 'Sources';
        case 'fragments': return 'Fragments';
        case 'auth': return 'Auth';
        case 'account': return 'Account';
        case 'home':
        default: return 'Home';
      }
    },
    isAdminPage() {
      const path = this.$route.path.toLowerCase();
      const segments = path.split('/').filter(s => s);
      const firstSegment = segments[0] || '';
      return firstSegment === 'admin' || firstSegment === 'integrations';
    }
  }
};
</script>
