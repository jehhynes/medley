// AI Prompts page - Entry point
// Imports and mounts the AiPrompts Vue SFC from the compiled app bundle
(function() {
    const { createApp } = Vue;

    // Wait for dependencies to load before mounting
    function initializeApp() {
        if (!window.AiPrompts || !window.TiptapEditor || !window.VerticalMenu) {
            setTimeout(initializeApp, 100);
            return;
        }

        const app = createApp(window.AiPrompts);
        
        // Register components globally for this app instance
        app.component('tiptap-editor', window.TiptapEditor);
        app.component('vertical-menu', window.VerticalMenu);
        
        app.mount('#app');
    }

    // Start initialization when script loads
    initializeApp();
})();
