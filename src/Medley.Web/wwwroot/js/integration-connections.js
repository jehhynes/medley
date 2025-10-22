/**
 * Integration Connection Testing Utilities
 * Shared JavaScript functionality for testing integration connections
 */

// Global configuration
window.IntegrationConnections = {
    // Default URLs - can be overridden by individual pages
    testConnectionUrl: '/Integrations/Manage/TestConnection',
    
    /**
     * Initialize test connection functionality for all elements with class 'test-connection'
     */
    init: function() {
        document.querySelectorAll('.test-connection').forEach(button => {
            button.addEventListener('click', this.handleTestConnection.bind(this));
        });
    },
    
    /**
     * Handle test connection button click
     */
    handleTestConnection: function(event) {
        const button = event.target.closest('.test-connection');
        const id = button.dataset.id;
        const icon = button.querySelector('i');
        
        // Show loading state
        icon.className = 'bi bi-arrow-clockwise';
        button.disabled = true;
        
        // Create form data
        const formData = new FormData();
        formData.append('id', id);
        
        // Get CSRF token
        const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]');
        if (csrfToken) {
            formData.append('__RequestVerificationToken', csrfToken.value);
        }
        
        // Make the request
        fetch(this.testConnectionUrl, {
            method: 'POST',
            body: formData
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Update status badge if it exists
                const statusBadge = document.getElementById('status-' + id);
                if (statusBadge) {
                    if (data.connected) {
                        statusBadge.className = 'badge bg-success';
                        statusBadge.textContent = 'Connected';
                    } else {
                        statusBadge.className = 'badge bg-danger';
                        statusBadge.textContent = 'Error';
                    }
                }
                
                // Show success message
                this.showAlert('Connection test completed successfully', 'success');
            } else {
                this.showAlert('Connection test failed: ' + data.error, 'danger');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            this.showAlert('An error occurred while testing the connection', 'danger');
        })
        .finally(() => {
            // Reset button state
            icon.className = 'bi bi-check-circle';
            button.disabled = false;
        });
    },
    
    /**
     * Show an alert message
     * @param {string} message - The message to display
     * @param {string} type - The alert type (success, danger, warning, info)
     */
    showAlert: function(message, type) {
        const alertDiv = document.createElement('div');
        alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
        alertDiv.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        `;
        
        // Insert at the top of the container
        const container = document.querySelector('.container-fluid') || document.querySelector('.container') || document.body;
        container.insertBefore(alertDiv, container.firstChild);
        
        // Auto-dismiss after 5 seconds
        setTimeout(() => {
            if (alertDiv.parentNode) {
                alertDiv.remove();
            }
        }, 5000);
    },
    
    /**
     * Set custom test connection URL
     * @param {string} url - The URL to use for test connection requests
     */
    setTestConnectionUrl: function(url) {
        this.testConnectionUrl = url;
    }
};

// Auto-initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    window.IntegrationConnections.init();
});
