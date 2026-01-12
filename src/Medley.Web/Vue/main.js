// Main entry point for Vue SPA
// Creates a single Vue app instance with Vue Router


  const { createApp } = await import('vue');
  const { default: App } = await import('./App.vue');
  const { default: router } = await import('./router');

  // Import third-party libraries
  const { default: bootbox } = await import('bootbox');
  const { marked } = await import('marked');

  // Import all components
  const { default: TiptapEditor } = await import('./components/TiptapEditor.vue');
  const { default: ArticleList } = await import('./components/ArticleList.vue');
  const { default: ArticleTree } = await import('./components/ArticleTree.vue');
  const { default: ChatPanel } = await import('./components/ChatPanel.vue');
  const { default: FragmentList } = await import('./components/FragmentList.vue');
  const { default: FragmentModal } = await import('./components/FragmentModal.vue');
  const { default: PlanViewer } = await import('./components/PlanViewer.vue');
  const { default: SourceList } = await import('./components/SourceList.vue');
  const { default: VerticalMenu } = await import('./components/VerticalMenu.vue');
  const { default: VersionsPanel } = await import('./components/VersionsPanel.vue');
  const { default: VirtualScroller } = await import('./components/VirtualScroller.vue');

  // Make libraries available globally (for backward compatibility)
  window.bootbox = bootbox;
  window.marked = marked;

  // Import json-viewer asynchronously (web component, doesn't need to block)
  //if (!customElements.get('json-viewer')) {
    import('@alenaksu/json-viewer');
  //}

  // Create the app
  const app = createApp(App);

  // Register all components globally
  app.component('tiptap-editor', TiptapEditor);
  app.component('article-list', ArticleList);
  app.component('article-tree', ArticleTree);
  app.component('chat-panel', ChatPanel);
  app.component('fragment-list', FragmentList);
  app.component('fragment-modal', FragmentModal);
  app.component('plan-viewer', PlanViewer);
  app.component('source-list', SourceList);
  app.component('vertical-menu', VerticalMenu);
  app.component('versions-panel', VersionsPanel);
  app.component('virtual-scroller', VirtualScroller);

  app.use(router);

  // Wait for router to be ready before mounting
  router.isReady().then(() => {
    app.mount('#app');
  });