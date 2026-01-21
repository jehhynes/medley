import type { HubConnection } from '@microsoft/signalr';

export interface ArticleCreatedPayload {
  articleId: string;
  title: string;
  parentArticleId: string | null;
  articleTypeId: string | null;
  timestamp: string;
}

export interface ArticleUpdatedPayload {
  articleId: string;
  title: string;
  articleTypeId: string | null;
  timestamp: string;
}

export interface ArticleDeletedPayload {
  articleId: string;
  timestamp: string;
}

export interface ArticleMovedPayload {
  articleId: string;
  oldParentId: string | null;
  newParentId: string | null;
  timestamp: string;
}

export interface ArticleAssignmentChangedPayload {
  articleId: string;
  userId: string | null;
  userName: string | null;
  userInitials: string | null;
  userColor: string | null;
  timestamp: string;
}

export interface VersionCreatedPayload {
  articleId: string;
  versionId: string;
  versionNumber: string;
  versionType: 'User' | 'AI';
  createdAt: string;
}

export interface PlanGeneratedPayload {
  articleId: string;
  planId: string;
  timestamp: string;
}

export interface ChatTurnStartedPayload {
  conversationId: string;
  articleId: string;
}

export interface ChatTurnCompletePayload {
  conversationId: string;
  articleId: string;
}

export interface ChatMessageStreamingPayload {
  conversationId: string;
  articleId: string;
  role: string;
  text: string | null;
  //toolName: string | null;
  //toolCallId: string | null;
  //toolDisplay: string | null;
  //toolResultIds: string[] | null;
  //isError: boolean | null;
  messageId: string | null;
  timestamp: string;
}

export interface ChatToolInvokedPayload {
  conversationId: string;
  articleId: string;
  toolName: string;
  toolCallId: string;
  toolDisplay: string | null;
  messageId: string;
  timestamp: string;
}

export interface ChatToolCompletedPayload {
  conversationId: string;
  articleId: string;
  toolCallId: string;
  toolResultIds: string[] | null;
  isError: boolean;
  messageId: string;
  timestamp: string;
}

export interface ChatMessageCompletePayload {
  messageId: string;
  conversationId: string;
  articleId: string;
  role: string;
  content: string;
  timestamp: string;
}

export interface ChatErrorPayload {
  conversationId: string;
  articleId: string;
  message: string;
  timestamp: string;
}

export interface ChatMessageReceivedPayload {
  messageId: string;
  conversationId: string;
  role: string;
  text: string;
  userName: string;
  createdAt: string;
  articleId: string;
}

export interface ConversationCompletedPayload {
  conversationId: string;
  articleId: string;
  completedAt: string;
}

export interface ConversationCancelledPayload {
  conversationId: string;
  articleId: string;
  timestamp: string;
}

export interface ArticleHubServerMethods {
  ArticleCreated: (payload: ArticleCreatedPayload) => void;
  ArticleUpdated: (payload: ArticleUpdatedPayload) => void;
  ArticleDeleted: (payload: ArticleDeletedPayload) => void;
  ArticleMoved: (payload: ArticleMovedPayload) => void;
  ArticleAssignmentChanged: (payload: ArticleAssignmentChangedPayload) => void;
  VersionCreated: (payload: VersionCreatedPayload) => void;
  PlanGenerated: (payload: PlanGeneratedPayload) => void;
  ChatTurnStarted: (payload: ChatTurnStartedPayload) => void;
  ChatTurnComplete: (payload: ChatTurnCompletePayload) => void;
  ChatMessageStreaming: (payload: ChatMessageStreamingPayload) => void;
  ChatToolInvoked: (payload: ChatToolInvokedPayload) => void;
  ChatToolCompleted: (payload: ChatToolCompletedPayload) => void;
  ChatMessageComplete: (payload: ChatMessageCompletePayload) => void;
  ChatError: (payload: ChatErrorPayload) => void;
  ChatMessageReceived: (payload: ChatMessageReceivedPayload) => void;
  ConversationCompleted: (payload: ConversationCompletedPayload) => void;
  ConversationCancelled: (payload: ConversationCancelledPayload) => void;
}

/** Typed hub connection with compile-time type safety for event handlers */
export type ArticleHubConnection = HubConnection & {
  on<K extends keyof ArticleHubServerMethods>(
    methodName: K,
    handler: ArticleHubServerMethods[K]
  ): void;
};
