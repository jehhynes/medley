// Home/Dashboard page - Entry point
// Imports and mounts the Dashboard Vue SFC from the compiled app bundle
(function() {
    const { createApp } = Vue;

    // Wait for dependencies to load before mounting
    function initializeApp() {
        if (!window.Dashboard || !window.VerticalMenu) {
            setTimeout(initializeApp, 100);
            return;
        }

        const app = createApp(window.Dashboard);
        
        // Register components globally for this app instance
        app.component('vertical-menu', window.VerticalMenu);
        
        app.mount('#app');
    }

    // Start initialization when script loads
    initializeApp();
})();
