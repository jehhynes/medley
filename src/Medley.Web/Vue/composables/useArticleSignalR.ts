import { ref, type Ref } from 'vue';
import { createArticleHubConnection, startConnection, stopConnection } from '@/utils/signalr';
import { debounce } from '@/utils/helpers';
import type { ArticleHubConnection } from '@/types/signalr/article-hub';
import type { ArticleDto, UserSummaryDto } from '@/types/generated/api-client';

/**
 * Maximum number of updates to queue before dropping oldest
 */
const MAX_QUEUE_SIZE = 100;

/**
 * SignalR queue update types
 */
type SignalRUpdateType = 
  | 'ArticleCreated'
  | 'ArticleUpdated'
  | 'ArticleAssignmentChanged'
  | 'ArticleDeleted'
  | 'ArticleMoved';

/**
 * Base interface for SignalR queue updates
 */
interface SignalRQueueUpdateBase {
  type: SignalRUpdateType;
}

/**
 * Article created update
 */
interface ArticleCreatedUpdate extends SignalRQueueUpdateBase {
  type: 'ArticleCreated';
  article: ArticleDto;
}

/**
 * Article updated update
 */
interface ArticleUpdatedUpdate extends SignalRQueueUpdateBase {
  type: 'ArticleUpdated';
  articleId: string;
  updates: Partial<ArticleDto>;
}

/**
 * Article assignment changed update
 */
interface ArticleAssignmentChangedUpdate extends SignalRQueueUpdateBase {
  type: 'ArticleAssignmentChanged';
  articleId: string;
  updates: {
    assignedUser: UserSummaryDto | null;
  };
}

/**
 * Article deleted update
 */
interface ArticleDeletedUpdate extends SignalRQueueUpdateBase {
  type: 'ArticleDeleted';
  articleId: string;
}

/**
 * Article moved update
 */
interface ArticleMovedUpdate extends SignalRQueueUpdateBase {
  type: 'ArticleMoved';
  articleId: string;
  oldParentId: string | null;
  newParentId: string | null;
}

/**
 * Union type for all SignalR queue updates
 */
export type SignalRQueueUpdate =
  | ArticleCreatedUpdate
  | ArticleUpdatedUpdate
  | ArticleAssignmentChangedUpdate
  | ArticleDeletedUpdate
  | ArticleMovedUpdate;

/**
 * Options for useArticleSignalR composable
 */
export interface UseArticleSignalROptions {
  /**
   * Function to insert an article into the tree
   */
  insertArticleIntoTree: (article: ArticleSummaryDto) => void;

  /**
   * Function to update an article in the tree
   */
  updateArticleInTree: (articleId: string, updates: Partial<ArticleDto>) => void;

  /**
   * Function to remove an article from the tree
   */
  removeArticleFromTree: (articleId: string) => void;

  /**
   * Function to move an article in the tree
   */
  moveArticleInTree: (articleId: string, oldParentId: string | null, newParentId: string | null) => void;

  /**
   * Function to open a plan tab
   */
  openPlanTab?: (planId: string) => void;

  /**
   * Function to open a version tab
   */
  openVersionTab?: (version: { id: string; versionNumber: number; createdAt: string }) => void;

  /**
   * Function to reload versions
   */
  loadVersions?: () => Promise<void>;

  /**
   * Function to rebuild My Work list cache
   */
  rebuildMyWorkListCache?: () => void;

  /**
   * Current selected article ID
   */
  selectedArticleId: Ref<string | null>;

  /**
   * Articles index map
   */
  articlesIndex: Map<string, ArticleDto>;

  /**
   * Function to clear selected article
   */
  clearSelectedArticle?: () => void;
}

/**
 * Return type for useArticleSignalR composable
 */
interface UseArticleSignalRReturn {
  connection: Ref<ArticleHubConnection | null>;
  updateQueue: Ref<SignalRQueueUpdate[]>;
  processing: Ref<boolean>;
  initializeConnection: () => Promise<void>;
  disconnectConnection: () => Promise<void>;
  joinArticle: (articleId: string) => Promise<void>;
  leaveArticle: (articleId: string) => Promise<void>;
}

/**
 * Composable for managing ArticleHub SignalR connection and real-time updates.
 * Handles connection lifecycle, event subscriptions, and update queue processing.
 * 
 * @param options - Configuration options for SignalR connection
 * @returns SignalR connection state and control methods
 */
export function useArticleSignalR(options: UseArticleSignalROptions): UseArticleSignalRReturn {
  const connection = ref<ArticleHubConnection | null>(null);
  const updateQueue = ref<SignalRQueueUpdate[]>([]);
  const processing = ref<boolean>(false);

  /**
   * Process queued SignalR updates in batch.
   * Batches rapid SignalR events (50ms window) to prevent UI thrashing.
   */
  const processSignalRQueue = (): void => {
    if (processing.value || updateQueue.value.length === 0) {
      return;
    }

    processing.value = true;

    // Process all queued updates
    const queue = [...updateQueue.value];
    updateQueue.value = [];

    queue.forEach(update => {
      switch (update.type) {
        case 'ArticleCreated':
          options.insertArticleIntoTree(update.article);
          break;

        case 'ArticleUpdated':
          options.updateArticleInTree(update.articleId, update.updates);
          break;

        case 'ArticleAssignmentChanged':
          options.updateArticleInTree(update.articleId, update.updates);
          break;

        case 'ArticleDeleted':
          options.removeArticleFromTree(update.articleId);
          if (options.selectedArticleId.value === update.articleId) {
            options.clearSelectedArticle?.();
          }
          break;

        case 'ArticleMoved':
          options.moveArticleInTree(update.articleId, update.oldParentId, update.newParentId);
          break;
      }
    });

    processing.value = false;
  };

  // Create debounced processor for SignalR events
  const processSignalRQueueDebounced = debounce(processSignalRQueue, 50);

  /**
   * Add an update to the queue with size limit
   */
  const queueUpdate = (update: SignalRQueueUpdate): void => {
    if (updateQueue.value.length >= MAX_QUEUE_SIZE) {
      console.warn('SignalR update queue full, dropping oldest updates');
      updateQueue.value.shift();
    }
    updateQueue.value.push(update);
    processSignalRQueueDebounced();
  };

  /**
   * Normalize ID for comparison (handle both string and GUID formats)
   */
  const normalizeId = (id: string | null | undefined): string | null => {
    return id ? id.toString().toLowerCase() : null;
  };

  /**
   * Initialize SignalR connection and event handlers
   */
  const initializeConnection = async (): Promise<void> => {
    connection.value = createArticleHubConnection();

    // Article created event
    connection.value.on('ArticleCreated', (data) => {
      queueUpdate({
        type: 'ArticleCreated',
        article: {
          id: data.articleId,
          title: data.title,
          parentArticleId: data.parentArticleId,
          articleTypeId: data.articleTypeId,
          children: []
        }
      });
    });

    // Article updated event
    connection.value.on('ArticleUpdated', (data) => {
      queueUpdate({
        type: 'ArticleUpdated',
        articleId: data.articleId,
        updates: {
          title: data.title,
          articleTypeId: data.articleTypeId
        }
      });
    });

    // Article assignment changed event
    connection.value.on('ArticleAssignmentChanged', (data) => {
      queueUpdate({
        type: 'ArticleAssignmentChanged',
        articleId: data.articleId,
        updates: {
          assignedUser: data.userId ? {
            id: data.userId,
            fullName: data.userName || undefined,
            initials: data.userInitials,
            color: data.userColor
          } : null
        }
      });
    });

    // Article deleted event
    connection.value.on('ArticleDeleted', (data) => {
      queueUpdate({
        type: 'ArticleDeleted',
        articleId: data.articleId
      });
    });

    // Article moved event
    connection.value.on('ArticleMoved', (data) => {
      queueUpdate({
        type: 'ArticleMoved',
        articleId: data.articleId,
        oldParentId: data.oldParentId,
        newParentId: data.newParentId
      });
    });

    // Version created event
    connection.value.on('VersionCreated', (data) => {
      // Refresh the versions panel if it's for the currently selected article
      if (options.selectedArticleId.value === data.articleId && options.loadVersions) {
        options.loadVersions();
      }
    });

    // Plan generated event
    connection.value.on('PlanGenerated', (data) => {
      const selectedId = normalizeId(options.selectedArticleId.value);
      const eventArticleId = normalizeId(data.articleId);

      console.log('PlanGenerated event received:', {
        eventArticleId,
        selectedId,
        planId: data.planId,
        matches: selectedId === eventArticleId
      });

      // Open plan tab when plan is generated for the currently selected article
      if (selectedId === eventArticleId && options.openPlanTab) {
        console.log('Opening plan tab automatically for plan:', data.planId);
        options.openPlanTab(data.planId);
      } else {
        console.log('Plan generated for different article, not opening tab');
      }
    });

    // Article version created event
    connection.value.on('ArticleVersionCreated', async (data) => {
      const selectedId = normalizeId(options.selectedArticleId.value);
      const eventArticleId = normalizeId(data.articleId);

      console.log('ArticleVersionCreated event received:', {
        eventArticleId,
        selectedId,
        versionId: data.versionId,
        versionNumber: data.versionNumber,
        matches: selectedId === eventArticleId
      });

      // Open version tab when version is created for the currently selected article
      if (selectedId === eventArticleId) {
        console.log('Opening version tab automatically for version:', data.versionId);
        
        // Reload the versions list
        if (options.loadVersions) {
          await options.loadVersions();
        }
        
        // Open and switch to the version tab
        if (options.openVersionTab) {
          options.openVersionTab({
            id: data.versionId,
            versionNumber: data.versionNumber,
            createdAt: data.timestamp
          });
        }
      } else {
        console.log('Version created for different article, not opening tab');
      }
    });

    // Chat turn started event
    connection.value.on('ChatTurnStarted', (data) => {
      const article = options.articlesIndex.get(options.selectedArticleId.value || '');
      if (article) {
        // Ensure currentConversation object exists
        if (!article.currentConversation) {
          article.currentConversation = { 
            id: data.conversationId, 
            isRunning: true 
          };
        } else {
          article.currentConversation.isRunning = true;
          article.currentConversation.id = data.conversationId;
        }
        
        // Rebuild My Work cache since conversation status changed
        options.rebuildMyWorkListCache?.();
      }
    });

    // Chat turn complete event
    connection.value.on('ChatTurnComplete', (data) => {
      const article = options.articlesIndex.get(options.selectedArticleId.value || '');
      if (article?.currentConversation) {
        article.currentConversation.isRunning = false;
        
        // Rebuild My Work cache since conversation status changed
        options.rebuildMyWorkListCache?.();
      }
    });

    // Handle reconnection
    connection.value.onreconnected(async (connectionId) => {
      console.log('SignalR reconnected. Re-joining article group.');
      if (options.selectedArticleId.value && connection.value) {
        try {
          await connection.value.invoke('JoinArticle', options.selectedArticleId.value);
        } catch (err) {
          console.error('Error re-joining article group after reconnection:', err);
        }
      }
    });

    // Start the connection
    await startConnection(connection.value);
    console.log('Connected to ArticleHub');
  };

  /**
   * Disconnect from SignalR
   */
  const disconnectConnection = async (): Promise<void> => {
    if (connection.value) {
      await stopConnection(connection.value);
      console.log('Disconnected from ArticleHub');
      connection.value = null;
    }
  };

  /**
   * Join an article's SignalR group
   */
  const joinArticle = async (articleId: string): Promise<void> => {
    if (connection.value) {
      try {
        await connection.value.invoke('JoinArticle', articleId);
        console.log('Joined article group:', articleId);
      } catch (err) {
        console.error('Error joining article group:', err);
      }
    }
  };

  /**
   * Leave an article's SignalR group
   */
  const leaveArticle = async (articleId: string): Promise<void> => {
    if (connection.value) {
      try {
        await connection.value.invoke('LeaveArticle', articleId);
        console.log('Left article group:', articleId);
      } catch (err) {
        console.error('Error leaving article group:', err);
      }
    }
  };

  return {
    connection,
    updateQueue,
    processing,
    initializeConnection,
    disconnectConnection,
    joinArticle,
    leaveArticle
  };
}
