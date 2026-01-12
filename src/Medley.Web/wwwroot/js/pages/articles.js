// Articles page - Entry point
// Imports and mounts the Articles Vue SFC from the compiled app bundle
(function() {
    const { createApp } = Vue;

    // Wait for dependencies to load before mounting
    function initializeApp() {
        if (!window.Articles || 
            !window.TiptapEditor || 
            !window.VerticalMenu || 
            !window.ArticleTree || 
            !window.ArticleList || 
            !window.ChatPanel || 
            !window.VersionsPanel || 
            !window.PlanViewer) {
            setTimeout(initializeApp, 100);
            return;
        }

        const app = createApp(window.Articles);
        
        // Register components globally for this app instance
        app.component('tiptap-editor', window.TiptapEditor);
        app.component('vertical-menu', window.VerticalMenu);
        app.component('article-tree', window.ArticleTree);
        app.component('article-list', window.ArticleList);
        app.component('chat-panel', window.ChatPanel);
        app.component('versions-panel', window.VersionsPanel);
        app.component('plan-viewer', window.PlanViewer);
        
        app.mount('#app');
    }

    // Start initialization when script loads
    initializeApp();
})();
