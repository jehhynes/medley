import { ref, type Ref } from 'vue';
import { createArticleHubConnection, startConnection, stopConnection } from '@/utils/signalr';
import { debounce } from '@/utils/helpers';
import type { ArticleHubConnection } from '../types/article-hub';
import type { ArticleDto, UserSummaryDto } from '@/types/api-client';

const MAX_QUEUE_SIZE = 100;

type SignalRUpdateType = 
  | 'ArticleCreated'
  | 'ArticleUpdated'
  | 'ArticleAssignmentChanged'
  | 'ArticleDeleted'
  | 'ArticleMoved';

interface SignalRQueueUpdateBase {
  type: SignalRUpdateType;
}

interface ArticleCreatedUpdate extends SignalRQueueUpdateBase {
  type: 'ArticleCreated';
  article: ArticleDto;
}

interface ArticleUpdatedUpdate extends SignalRQueueUpdateBase {
  type: 'ArticleUpdated';
  articleId: string;
  updates: Partial<ArticleDto>;
}

interface ArticleAssignmentChangedUpdate extends SignalRQueueUpdateBase {
  type: 'ArticleAssignmentChanged';
  articleId: string;
  updates: {
    assignedUser: UserSummaryDto | null;
  };
}

interface ArticleDeletedUpdate extends SignalRQueueUpdateBase {
  type: 'ArticleDeleted';
  articleId: string;
}

interface ArticleMovedUpdate extends SignalRQueueUpdateBase {
  type: 'ArticleMoved';
  articleId: string;
  oldParentId: string | null;
  newParentId: string | null;
}

export type SignalRQueueUpdate =
  | ArticleCreatedUpdate
  | ArticleUpdatedUpdate
  | ArticleAssignmentChangedUpdate
  | ArticleDeletedUpdate
  | ArticleMovedUpdate;

export interface UseArticleSignalROptions {
  insertArticleIntoTree: (article: ArticleSummaryDto) => void;
  updateArticleInTree: (articleId: string, updates: Partial<ArticleDto>) => void;
  removeArticleFromTree: (articleId: string) => void;
  moveArticleInTree: (articleId: string, oldParentId: string | null, newParentId: string | null) => void;
  openPlanTab?: (planId: string) => void;
  reloadPlan?: (planId: string) => void;
  openVersionTab?: (version: { id: string; versionNumber: number; createdAt: string }) => void;
  onVersionCreated?: (version: any) => Promise<void>;
  onVersionUpdated?: (version: any) => void;
  onVersionDeleted?: (versionId: string) => void;
  selectedArticleId: Ref<string | null>;
  articlesIndex: Map<string, ArticleDto>;
  clearSelectedArticle?: () => void;
}

/** Manages ArticleHub SignalR connection and real-time updates with batched queue processing */
export function useArticleSignalR(options: UseArticleSignalROptions) {
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
    connection.value.on('VersionCreated', async (data) => {
      const selectedId = normalizeId(options.selectedArticleId.value);
      const eventArticleId = normalizeId(data.articleId);

      console.log('VersionCreated event received:', {
        eventArticleId,
        selectedId,
        versionId: data.versionId,
        versionNumber: data.versionNumber,
        versionType: data.versionType,
        matches: selectedId === eventArticleId
      });

      // Reload versions if it's for the currently selected article
      if (selectedId === eventArticleId) {
        // Reload the full version list to get complete version data
        if (options.onVersionCreated) {
          await options.onVersionCreated(data);
        }

        // Auto-open AI versions after reload completes
        if (data.versionType === 'AI' && options.openVersionTab) {
          console.log('Auto-opening AI version tab:', data.versionId);
          options.openVersionTab({
            id: data.versionId,
            versionNumber: data.versionNumber,
            createdAt: data.createdAt
          });
        }
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

    // Plan updated event
    connection.value.on('PlanUpdated', (data) => {
      const selectedId = normalizeId(options.selectedArticleId.value);
      const eventArticleId = normalizeId(data.articleId);

      console.log('PlanUpdated event received:', {
        eventArticleId,
        selectedId,
        planId: data.planId,
        knowledgeUnitsAdded: data.knowledgeUnitsAdded,
        matches: selectedId === eventArticleId
      });

      // Reload plan when knowledge units are added for the currently selected article
      if (selectedId === eventArticleId && options.reloadPlan) {
        console.log('Reloading plan after knowledge units added:', data.planId);
        options.reloadPlan(data.planId);
      } else {
        console.log('Plan updated for different article, not reloading');
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
      }
    });

    // Chat turn complete event
    connection.value.on('ChatTurnComplete', (data) => {
      const article = options.articlesIndex.get(options.selectedArticleId.value || '');
      if (article?.currentConversation) {
        article.currentConversation.isRunning = false;
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
