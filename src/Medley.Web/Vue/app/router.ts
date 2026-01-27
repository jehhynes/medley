import { createRouter, createWebHistory, type RouteRecordRaw, type NavigationGuardNext, type RouteLocationNormalized } from 'vue-router';

// Define custom route meta interface
interface RouteMeta {
  title?: string;
  hasLeftSidebar?: boolean;
  hasRightSidebar?: boolean;
}

// Lazy-load pages for code splitting
const Dashboard = () => import('../features/dashboard/pages/Dashboard.vue');
const Sources = () => import('../features/sources/pages/Sources.vue');
const Fragments = () => import('../features/sources/pages/Fragments.vue');
const Articles = () => import('../features/articles/pages/Articles.vue');
const AiPrompts = () => import('../features/admin/pages/AiPrompts.vue');
const Speakers = () => import('../features/admin/pages/Speakers.vue');
const TokenUsage = () => import('../features/admin/pages/TokenUsage.vue');

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'dashboard',
    component: Dashboard,
    meta: { 
      title: 'Dashboard',
      hasLeftSidebar: false
    } as RouteMeta
  },
  {
    path: '/Sources',
    name: 'sources',
    component: Sources,
    meta: { 
      title: 'Sources'
    } as RouteMeta,
    props: route => ({ id: route.query.id })
  },
  {
    path: '/Fragments',
    name: 'fragments',
    component: Fragments,
    meta: { 
      title: 'Fragments'
    } as RouteMeta,
    props: route => ({ id: route.query.id })
  },
  {
    path: '/Articles',
    name: 'articles',
    component: Articles,
    meta: { 
      title: 'Articles',
      hasRightSidebar: true
    } as RouteMeta,
    props: route => ({ id: route.query.id })
  },
  {
    path: '/Admin/AiPrompts',
    name: 'ai-prompts',
    component: AiPrompts,
    meta: { 
        title: 'AI Prompts'
    } as RouteMeta
  },
  {
    path: '/Admin/Speakers',
    name: 'speakers',
    component: Speakers,
    meta: { 
        title: 'Speakers'
    } as RouteMeta,
    props: route => ({ id: route.query.id })
  },
  {
    path: '/Admin/TokenUsage',
    name: 'token-usage',
    component: TokenUsage,
    meta: { 
        title: 'Token Usage'
    } as RouteMeta
  }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

// Update document title on route change with typed navigation guard
router.afterEach((to: RouteLocationNormalized) => {
  const meta = to.meta as RouteMeta;
  document.title = meta.title ? `${meta.title} - Medley` : 'Medley';
});

export default router;
