// Article Browser Vue App
// Main initialization and utilities

// Global app state store (simple reactive object)
// This store can be shared across multiple Vue app instances
const createStore = () => {
    return Vue.reactive({
        articles: [],
        sources: [],
        selectedArticle: null,
        selectedSource: null,
        loading: false,
        error: null
    });
};

// Create a shared reactive store for Articles page
const createArticlesStore = () => {
    return Vue.reactive({
        articles: [],
        selectedArticleId: null,
        selectedArticle: null,
        loading: false,
        error: null
    });
};

// Create a shared reactive store for Sources page
const createSourcesStore = () => {
    return Vue.reactive({
        sources: [],
        selectedSourceId: null,
        selectedSource: null,
        loading: false,
        error: null,
        searchQuery: ''
    });
};

// API utilities
const api = {
    async get(url) {
        const response = await fetch(url, {
            headers: {
                'Content-Type': 'application/json'
            }
        });
        
        if (!response.ok) {
            throw new Error(`API error: ${response.statusText}`);
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

// SignalR connection factory
const createSignalRConnection = (hubUrl) => {
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

// Export utilities
window.ArticleBrowserApp = {
    createStore,
    createArticlesStore,
    createSourcesStore,
    api,
    createSignalRConnection,
    // Utils will be loaded from utils.js and accessed via window.MedleyUtils
    get utils() {
        return window.MedleyUtils || {};
    }
};

