// Dropdown Mixin - Handles closing other dropdowns when one is opened
(function() {
    window.dropdownMixin = {
        methods: {
            /**
             * Close all open dropdowns globally, excluding a specific button
             * Finds the root sidebar container and closes all dropdowns within it
             * This ensures dropdowns are closed across all component instances (including recursive ones)
             * @param {HTMLElement} excludeButton - Dropdown button to exclude from closing
             */
            closeAllDropdowns(excludeButton = null) {
                // Find the root sidebar container (works for both tree and list views)
                const sidebar = this.$el.closest('.sidebar-content') || this.$el.closest('.sidebar');
                if (!sidebar) {
                    // Fallback: search from document root
                    const sidebarContent = document.querySelector('.sidebar-content');
                    if (sidebarContent) {
                        const dropdownButtons = sidebarContent.querySelectorAll('[data-bs-toggle="dropdown"]');
                        dropdownButtons.forEach(button => {
                            if (button !== excludeButton) {
                                const dropdown = bootstrap.Dropdown.getInstance(button);
                                if (dropdown && dropdown._isShown()) {
                                    dropdown.hide();
                                }
                            }
                        });
                    }
                    return;
                }
                
                // Find all dropdown buttons in the sidebar
                const dropdownButtons = sidebar.querySelectorAll('[data-bs-toggle="dropdown"]');
                dropdownButtons.forEach(button => {
                    if (button !== excludeButton) {
                        const dropdown = bootstrap.Dropdown.getInstance(button);
                        if (dropdown && dropdown._isShown()) {
                            dropdown.hide();
                        }
                    }
                });
            },
            
            /**
             * Handle dropdown button click
             * Closes all other dropdowns before allowing Bootstrap to toggle the clicked one
             * @param {Event} event - Click event
             * @param {string} articleId - Article ID for the dropdown (optional, for logging/debugging)
             */
            handleDropdownClick(event, articleId) {
                // Close all other dropdowns first (excluding the clicked one)
                const clickedButton = event.target.closest('[data-bs-toggle="dropdown"]');
                this.closeAllDropdowns(clickedButton);
                // Let Bootstrap handle the toggle for this dropdown
                // The event will bubble and Bootstrap will handle it via data-bs-toggle
            }
        }
    };
})();

