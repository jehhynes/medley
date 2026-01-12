import { createRouter, createWebHistory } from 'vue-router';

// Lazy-load pages for code splitting
const Dashboard = () => import('../pages/Dashboard.vue');
const Sources = () => import('../pages/Sources.vue');
const Fragments = () => import('../pages/Fragments.vue');
const Articles = () => import('../pages/Articles.vue');
const AiPrompts = () => import('../pages/AiPrompts.vue');

const routes = [
  {
    path: '/',
    name: 'dashboard',
    component: Dashboard,
    meta: { 
      title: 'Dashboard',
      hasLeftSidebar: false
    }
  },
  {
    path: '/Sources',
    name: 'sources',
    component: Sources,
    meta: { 
      title: 'Sources'
    }
  },
  {
    path: '/Fragments',
    name: 'fragments',
    component: Fragments,
    meta: { 
      title: 'Fragments'
    }
  },
  {
    path: '/Articles',
    name: 'articles',
    component: Articles,
    meta: { 
      title: 'Articles',
      hasRightSidebar: true
    }
  },
  {
    path: '/Admin/AiPrompts',
    name: 'ai-prompts',
    component: AiPrompts,
    meta: { 
        title: 'AI Prompts'
    }
  }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

// Update document title on route change
router.afterEach((to) => {
  document.title = to.meta.title ? `${to.meta.title} - Medley` : 'Medley';
});

export default router;
