// Sources page - Entry point
// Imports and mounts the Sources Vue SFC from the compiled app bundle
(function() {
    const { createApp } = Vue;

    // Wait for dependencies to load before mounting
    function initializeApp() {
        if (!window.Sources || !window.SourceList || !window.FragmentModal || !window.VerticalMenu) {
            setTimeout(initializeApp, 100);
            return;
        }

        const app = createApp(window.Sources);
        
        // Register components globally for this app instance
        app.component('source-list', window.SourceList);
        app.component('fragment-modal', window.FragmentModal);
        app.component('vertical-menu', window.VerticalMenu);
        
        app.mount('#app');
    }

    // Start initialization when script loads
    initializeApp();
})();
