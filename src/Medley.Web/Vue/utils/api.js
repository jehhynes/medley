/**
 * API and SignalR utilities for Medley
 * ES6 module version
 */

/**
 * API client for making HTTP requests
 */
export const api = {
    async get(url) {
        const response = await fetch(url, {
            headers: {
                'Content-Type': 'application/json'
            }
        });
        
        if (!response.ok) {
            throw new Error(`API error: ${response.statusText}`);
        }
        
        // Handle 204 No Content responses
        if (response.status === 204) {
            return null;
        }
        
        return response.json();
    },
    
    async post(url, data) {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        
        if (!response.ok) {
            throw new Error(`API error: ${response.statusText}`);
        }
        
        // Handle 204 No Content responses
        if (response.status === 204) {
            return null;
        }
        
        return response.json();
    },
    
    async put(url, data) {
        const response = await fetch(url, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        
        if (!response.ok) {
            throw new Error(`API error: ${response.statusText}`);
        }
        
        // Handle 204 No Content responses
        if (response.status === 204) {
            return null;
        }
        
        return response.json();
    },
    
    async delete(url) {
        const response = await fetch(url, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        
        if (!response.ok) {
            throw new Error(`API error: ${response.statusText}`);
        }
        
        return response.ok;
    }
};

/**
 * SignalR connection factory
 * @param {string} hubUrl - The SignalR hub URL
 * @returns {signalR.HubConnection} Configured SignalR connection
 */
export const createSignalRConnection = (hubUrl) => {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .build();
    
    connection.onreconnecting((error) => {
        console.log('SignalR reconnecting...', error);
    });
    
    connection.onreconnected((connectionId) => {
        console.log('SignalR reconnected:', connectionId);
    });
    
    connection.onclose((error) => {
        console.log('SignalR connection closed:', error);
    });
    
    return connection;
};
