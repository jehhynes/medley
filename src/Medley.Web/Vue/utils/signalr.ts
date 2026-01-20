import * as signalR from '@microsoft/signalr';
import type { ArticleHubConnection } from '@/features/articles/types/article-hub';
import type { AdminHubConnection } from '@/types/admin-hub';

export function createArticleHubConnection(): ArticleHubConnection {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/articleHub')
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (retryContext) => {
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

export function createAdminHubConnection(): AdminHubConnection {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/adminHub')
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (retryContext) => {
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

      await new Promise(resolve => setTimeout(resolve, retryDelay));
    }
  }
}

export async function stopConnection(connection: signalR.HubConnection): Promise<void> {
  try {
    await connection.stop();
    console.log('[SignalR] Connection stopped successfully');
  } catch (error) {
    console.error('[SignalR] Error stopping connection:', error);
    throw error;
  }
}

export function getConnectionState(connection: signalR.HubConnection): signalR.HubConnectionState {
  return connection.state;
}

export function isConnected(connection: signalR.HubConnection): boolean {
  return connection.state === signalR.HubConnectionState.Connected;
}

export function isDisconnected(connection: signalR.HubConnection): boolean {
  return connection.state === signalR.HubConnectionState.Disconnected;
}

export function isReconnecting(connection: signalR.HubConnection): boolean {
  return connection.state === signalR.HubConnectionState.Reconnecting;
}
