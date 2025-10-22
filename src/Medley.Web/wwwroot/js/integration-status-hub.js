/**
 * SignalR Integration Status Updates
 * Real-time status updates for integration connections
 */

window.IntegrationStatusHub = {
    connection: null,
    isConnected: false,
    reconnectAttempts: 0,
    maxReconnectAttempts: 5,
    reconnectDelay: 2000, // Start with 2 seconds
    
    /**
     * Initialize SignalR connection
     */
    init: function() {
        // Check if SignalR is available
        if (typeof signalR === 'undefined') {
            console.warn('SignalR not available, skipping real-time updates');
            return;
        }
        
        // Create connection
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/integrationStatusHub')
            .withAutomaticReconnect([0, 2000, 10000, 30000]) // Progressive reconnection delays
            .build();
        
        // Set up event handlers
        this.setupEventHandlers();
        
        // Start connection
        this.start();
    },
    
    /**
     * Set up SignalR event handlers
     */
    setupEventHandlers: function() {
        const self = this;
        
        // Connection events
        this.connection.onclose(function(error) {
            self.isConnected = false;
            console.log('SignalR connection closed', error);
            self.showConnectionStatus('disconnected');
        });
        
        this.connection.onreconnecting(function(error) {
            self.isConnected = false;
            console.log('SignalR reconnecting...', error);
            self.showConnectionStatus('reconnecting');
        });
        
        this.connection.onreconnected(function(connectionId) {
            self.isConnected = true;
            self.reconnectAttempts = 0;
            console.log('SignalR reconnected with connection ID:', connectionId);
            self.showConnectionStatus('connected');
        });
        
        // Integration status update events
        this.connection.on('IntegrationStatusUpdate', function(integrationId, status, message) {
            self.handleStatusUpdate(integrationId, status, message);
        });
    },
    
    /**
     * Start SignalR connection
     */
    start: function() {
        const self = this;
        
        this.connection.start()
            .then(function() {
                self.isConnected = true;
                self.reconnectAttempts = 0;
                console.log('SignalR connection started');
                self.showConnectionStatus('connected');
                
                // Join the integration status group
                return self.connection.invoke('JoinIntegrationStatusGroup');
            })
            .then(function() {
                console.log('Joined integration status group');
            })
            .catch(function(error) {
                console.error('Error starting SignalR connection:', error);
                self.showConnectionStatus('error');
                self.handleConnectionError();
            });
    },
    
    /**
     * Handle connection errors
     */
    handleConnectionError: function() {
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            const delay = this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1); // Exponential backoff
            
            console.log(`Attempting to reconnect in ${delay}ms (attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts})`);
            
            // Show reconnection status
            this.showReconnectionStatus(this.reconnectAttempts, this.maxReconnectAttempts, delay);
            
            setTimeout(() => {
                this.start();
            }, delay);
        } else {
            console.error('Max reconnection attempts reached');
            this.showConnectionStatus('failed');
            this.showReconnectionFailed();
        }
    },
    
    /**
     * Show reconnection status to user
     */
    showReconnectionStatus: function(attempt, maxAttempts, delay) {
        const indicator = document.getElementById('signalr-reconnect-indicator');
        if (indicator) {
            indicator.remove();
        }
        
        const reconnectDiv = document.createElement('div');
        reconnectDiv.id = 'signalr-reconnect-indicator';
        reconnectDiv.className = 'position-fixed top-0 start-0 p-2';
        reconnectDiv.style.zIndex = '9999';
        
        const seconds = Math.ceil(delay / 1000);
        reconnectDiv.innerHTML = `
            <div class="alert alert-warning alert-sm mb-0 d-flex align-items-center" role="alert">
                <div class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></div>
                <small>Reconnecting... (${attempt}/${maxAttempts}) - Retry in ${seconds}s</small>
            </div>
        `;
        
        document.body.appendChild(reconnectDiv);
    },
    
    /**
     * Show reconnection failed message
     */
    showReconnectionFailed: function() {
        const indicator = document.getElementById('signalr-reconnect-indicator');
        if (indicator) {
            indicator.remove();
        }
        
        const failedDiv = document.createElement('div');
        failedDiv.id = 'signalr-reconnect-indicator';
        failedDiv.className = 'position-fixed top-0 start-0 p-2';
        failedDiv.style.zIndex = '9999';
        
        failedDiv.innerHTML = `
            <div class="alert alert-danger alert-sm mb-0 d-flex align-items-center" role="alert">
                <i class="bi bi-exclamation-triangle me-2"></i>
                <small>Real-time updates failed. <button class="btn btn-sm btn-outline-danger ms-2" onclick="window.IntegrationStatusHub.manualReconnect()">Retry</button></small>
            </div>
        `;
        
        document.body.appendChild(failedDiv);
    },
    
    /**
     * Manual reconnection attempt
     */
    manualReconnect: function() {
        this.reconnectAttempts = 0;
        this.showConnectionStatus('reconnecting');
        this.start();
    },
    
    /**
     * Handle integration status updates
     */
    handleStatusUpdate: function(integrationId, status, message) {
        console.log('Status update received:', integrationId, status, message);
        
        // Update status badge
        const statusBadge = document.getElementById('status-' + integrationId);
        if (statusBadge) {
            const statusClass = this.getStatusClass(status);
            statusBadge.className = `badge ${statusClass}`;
            statusBadge.textContent = status;
            
            // Add animation
            statusBadge.classList.add('animate__animated', 'animate__pulse');
            setTimeout(() => {
                statusBadge.classList.remove('animate__animated', 'animate__pulse');
            }, 1000);
        }
        
        // Update status message
        const statusMessage = document.getElementById('status-message-' + integrationId);
        if (statusMessage && message) {
            statusMessage.textContent = message;
            statusMessage.style.display = 'block';
            
            // Hide message after 5 seconds
            setTimeout(() => {
                statusMessage.style.display = 'none';
            }, 5000);
        }
        
        // Show notification if message provided
        if (message) {
            const alertType = this.getAlertType(status);
            window.IntegrationConnections.showAlert(message, alertType);
        }
    },
    
    /**
     * Get CSS class for status badge
     */
    getStatusClass: function(status) {
        switch (status.toLowerCase()) {
            case 'connected':
                return 'bg-success';
            case 'disconnected':
                return 'bg-warning';
            case 'error':
                return 'bg-danger';
            default:
                return 'bg-secondary';
        }
    },
    
    /**
     * Get alert type for status
     */
    getAlertType: function(status) {
        switch (status.toLowerCase()) {
            case 'connected':
                return 'success';
            case 'disconnected':
                return 'warning';
            case 'error':
                return 'danger';
            default:
                return 'info';
        }
    },
    
    /**
     * Show connection status indicator
     */
    showConnectionStatus: function(status) {
        // Remove existing status indicator
        const existingIndicator = document.getElementById('signalr-status-indicator');
        if (existingIndicator) {
            existingIndicator.remove();
        }
        
        // Create new status indicator
        const indicator = document.createElement('div');
        indicator.id = 'signalr-status-indicator';
        indicator.className = 'position-fixed top-0 end-0 p-2';
        indicator.style.zIndex = '9999';
        
        let iconClass, text, bgClass;
        switch (status) {
            case 'connected':
                iconClass = 'bi bi-wifi';
                text = 'Real-time updates connected';
                bgClass = 'bg-success';
                break;
            case 'reconnecting':
                iconClass = 'bi bi-wifi-off';
                text = 'Reconnecting...';
                bgClass = 'bg-warning';
                break;
            case 'disconnected':
                iconClass = 'bi bi-wifi-off';
                text = 'Real-time updates disconnected';
                bgClass = 'bg-secondary';
                break;
            case 'error':
            case 'failed':
                iconClass = 'bi bi-exclamation-triangle';
                text = 'Real-time updates failed';
                bgClass = 'bg-danger';
                break;
            default:
                iconClass = 'bi bi-question-circle';
                text = 'Real-time updates unknown';
                bgClass = 'bg-secondary';
        }
        
        indicator.innerHTML = `
            <div class="alert ${bgClass} alert-sm mb-0 d-flex align-items-center" role="alert">
                <i class="${iconClass} me-2"></i>
                <small>${text}</small>
            </div>
        `;
        
        document.body.appendChild(indicator);
        
        // Auto-hide after 3 seconds for success, 5 seconds for others
        const hideDelay = status === 'connected' ? 3000 : 5000;
        setTimeout(() => {
            if (indicator.parentNode) {
                indicator.remove();
            }
        }, hideDelay);
    },
    
    /**
     * Manually trigger a health check for an integration
     */
    triggerHealthCheck: function(integrationId) {
        if (this.isConnected && this.connection) {
            // This would typically be handled by the server-side health check job
            // For now, we'll just log it
            console.log('Triggering health check for integration:', integrationId);
        }
    },
    
    /**
     * Disconnect SignalR connection
     */
    disconnect: function() {
        if (this.connection) {
            this.connection.stop();
            this.isConnected = false;
            console.log('SignalR connection stopped');
        }
    }
};

// Auto-initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    // Only initialize if we're on an integration page
    if (document.querySelector('.test-connection') || window.location.pathname.includes('/Integrations')) {
        window.IntegrationStatusHub.init();
    }
});
