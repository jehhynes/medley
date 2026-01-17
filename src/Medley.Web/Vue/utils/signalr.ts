/**
 * Typed SignalR connection utilities for Medley
 * Provides factory functions for creating strongly-typed SignalR hub connections
 */

import * as signalR from '@microsoft/signalr';
import type { ArticleHubConnection } from '@/types/signalr/article-hub';
import type { AdminHubConnection } from '@/types/signalr/admin-hub';

/**
 * Create a typed connection to the ArticleHub
 * Provides compile-time type safety for all server-to-client event handlers
 * 
 * @returns Typed ArticleHub connection with automatic reconnection
 * 
 * @example
 * ```typescript
 * const connection = createArticleHubConnection();
 * 
 * // Type-safe event handlers
 * connection.on('ArticleCreated', (payload) => {
 *   console.log('Article created:', payload.articleId, payload.title);
 * });
 * 
 * await connection.start();
 * ```
 */
export function createArticleHubConnection(): ArticleHubConnection {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/articleHub')
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (retryContext) => {
        // Exponential backoff: 0s, 2s, 10s, 30s, then 30s for all subsequent attempts
        if (retryContext.previousRetryCount === 0) {
          return 0;
        } else if (retryContext.previousRetryCount === 1) {
          return 2000;
        } else if (retryContext.previousRetryCount === 2) {
          return 10000;
        } else {
          return 30000;
        }
      }
    })
    .configureLogging(signalR.LogLevel.Information)
    .build() as ArticleHubConnection;

  // Connection lifecycle event handlers
  connection.onreconnecting((error) => {
    console.log('[ArticleHub] Reconnecting...', error?.message || 'Connection lost');
  });

  connection.onreconnected((connectionId) => {
    console.log('[ArticleHub] Reconnected successfully', connectionId);
  });

  connection.onclose((error) => {
    if (error) {
      console.error('[ArticleHub] Connection closed with error:', error.message);
    } else {
      console.log('[ArticleHub] Connection closed');
    }
  });

  return connection;
}

/**
 * Create a typed connection to the AdminHub
 * Provides compile-time type safety for all server-to-client event handlers
 * 
 * @returns Typed AdminHub connection with automatic reconnection
 * 
 * @example
 * ```typescript
 * const connection = createAdminHubConnection();
 * 
 * // Type-safe event handlers
 * connection.on('IntegrationStatusUpdate', (payload) => {
 *   console.log('Integration status:', payload.status, payload.message);
 * });
 * 
 * await connection.start();
 * ```
 */
export function createAdminHubConnection(): AdminHubConnection {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/adminHub')
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (retryContext) => {
        // Exponential backoff: 0s, 2s, 10s, 30s, then 30s for all subsequent attempts
        if (retryContext.previousRetryCount === 0) {
          return 0;
        } else if (retryContext.previousRetryCount === 1) {
          return 2000;
        } else if (retryContext.previousRetryCount === 2) {
          return 10000;
        } else {
          return 30000;
        }
      }
    })
    .configureLogging(signalR.LogLevel.Information)
    .build() as AdminHubConnection;

  // Connection lifecycle event handlers
  connection.onreconnecting((error) => {
    console.log('[AdminHub] Reconnecting...', error?.message || 'Connection lost');
  });

  connection.onreconnected((connectionId) => {
    console.log('[AdminHub] Reconnected successfully', connectionId);
  });

  connection.onclose((error) => {
    if (error) {
      console.error('[AdminHub] Connection closed with error:', error.message);
    } else {
      console.log('[AdminHub] Connection closed');
    }
  });

  return connection;
}

/**
 * Helper function to safely start a SignalR connection
 * Handles connection errors and provides retry logic
 * 
 * @param connection - The SignalR connection to start
 * @param maxRetries - Maximum number of retry attempts (default: 3)
 * @param retryDelay - Delay between retries in milliseconds (default: 2000)
 * @returns Promise that resolves when connection is established
 * 
 * @example
 * ```typescript
 * const connection = createArticleHubConnection();
 * await startConnection(connection);
 * ```
 */
export async function startConnection(
  connection: signalR.HubConnection,
  maxRetries: number = 3,
  retryDelay: number = 2000
): Promise<void> {
  let retryCount = 0;

  while (retryCount < maxRetries) {
    try {
      await connection.start();
      console.log('[SignalR] Connection started successfully');
      return;
    } catch (error) {
      retryCount++;
      console.error(`[SignalR] Connection attempt ${retryCount} failed:`, error);

      if (retryCount >= maxRetries) {
        throw new Error(`Failed to start SignalR connection after ${maxRetries} attempts`);
      }

      // Wait before retrying
      await new Promise(resolve => setTimeout(resolve, retryDelay));
    }
  }
}

/**
 * Helper function to safely stop a SignalR connection
 * 
 * @param connection - The SignalR connection to stop
 * @returns Promise that resolves when connection is stopped
 * 
 * @example
 * ```typescript
 * await stopConnection(connection);
 * ```
 */
export async function stopConnection(connection: signalR.HubConnection): Promise<void> {
  try {
    await connection.stop();
    console.log('[SignalR] Connection stopped successfully');
  } catch (error) {
    console.error('[SignalR] Error stopping connection:', error);
    throw error;
  }
}

/**
 * Get the current connection state
 * 
 * @param connection - The SignalR connection
 * @returns The current connection state
 */
export function getConnectionState(connection: signalR.HubConnection): signalR.HubConnectionState {
  return connection.state;
}

/**
 * Check if connection is connected
 * 
 * @param connection - The SignalR connection
 * @returns True if connected, false otherwise
 */
export function isConnected(connection: signalR.HubConnection): boolean {
  return connection.state === signalR.HubConnectionState.Connected;
}

/**
 * Check if connection is disconnected
 * 
 * @param connection - The SignalR connection
 * @returns True if disconnected, false otherwise
 */
export function isDisconnected(connection: signalR.HubConnection): boolean {
  return connection.state === signalR.HubConnectionState.Disconnected;
}

/**
 * Check if connection is reconnecting
 * 
 * @param connection - The SignalR connection
 * @returns True if reconnecting, false otherwise
 */
export function isReconnecting(connection: signalR.HubConnection): boolean {
  return connection.state === signalR.HubConnectionState.Reconnecting;
}
