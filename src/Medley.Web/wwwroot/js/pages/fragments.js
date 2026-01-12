// Fragments page - Entry point
// Imports and mounts the Fragments Vue SFC from the compiled app bundle
(function() {
    const { createApp } = Vue;

    // Wait for dependencies to load before mounting
    function initializeApp() {
        if (!window.Fragments || !window.FragmentList || !window.VerticalMenu) {
            setTimeout(initializeApp, 100);
            return;
        }

        const app = createApp(window.Fragments);
        
        // Register components globally for this app instance
        app.component('fragment-list', window.FragmentList);
        app.component('vertical-menu', window.VerticalMenu);
        
        app.mount('#app');
    }

    // Start initialization when script loads
    initializeApp();
})();

