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
     * Show a toast notification
     * @param {string} message - The message to display
     * @param {string} type - The toast type (success, danger, warning, info)
     */
    showAlert: function(message, type) {
        // Create toast element
        const toastId = 'toast-' + Date.now();
        const toastElement = document.createElement('div');
        toastElement.id = toastId;
        toastElement.className = 'toast';
        toastElement.setAttribute('role', 'alert');
        toastElement.setAttribute('aria-live', 'assertive');
        toastElement.setAttribute('aria-atomic', 'true');
        
        // Set toast content based on type
        let iconClass = '';
        let title = '';
        switch(type) {
            case 'success':
                iconClass = 'bi bi-check-circle-fill text-success';
                title = 'Success';
                break;
            case 'danger':
                iconClass = 'bi bi-exclamation-triangle-fill text-danger';
                title = 'Error';
                break;
            case 'warning':
                iconClass = 'bi bi-exclamation-triangle-fill text-warning';
                title = 'Warning';
                break;
            case 'info':
                iconClass = 'bi bi-info-circle-fill text-info';
                title = 'Information';
                break;
            default:
                iconClass = 'bi bi-info-circle-fill text-primary';
                title = 'Notification';
        }
        
        toastElement.innerHTML = `
            <div class="toast-header">
                <i class="${iconClass} me-2"></i>
                <strong class="me-auto">${title}</strong>
                <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        `;
        
        // Add to toast container
        const toastContainer = document.querySelector('.toast-container');
        if (toastContainer) {
            toastContainer.appendChild(toastElement);
            
            // Initialize and show the toast
            const toast = new bootstrap.Toast(toastElement, {
                autohide: true,
                delay: 5000
            });
            toast.show();
            
            // Remove the toast element after it's hidden
            toastElement.addEventListener('hidden.bs.toast', function() {
                toastElement.remove();
            });
        }
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
