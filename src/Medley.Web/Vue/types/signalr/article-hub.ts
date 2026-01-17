import type { HubConnection } from '@microsoft/signalr';

/**
 * Payload for ArticleCreated event
 * Sent when a new article is created
 */
export interface ArticleCreatedPayload {
  articleId: string;
  title: string;
  parentArticleId: string | null;
  articleTypeId: string | null;
  timestamp: string;
}

/**
 * Payload for ArticleUpdated event
 * Sent when an article is updated
 */
export interface ArticleUpdatedPayload {
  articleId: string;
  title: string;
  articleTypeId: string | null;
  timestamp: string;
}

/**
 * Payload for ArticleDeleted event
 * Sent when an article is deleted
 */
export interface ArticleDeletedPayload {
  articleId: string;
  timestamp: string;
}

/**
 * Payload for ArticleMoved event
 * Sent when an article is moved to a different parent
 */
export interface ArticleMovedPayload {
  articleId: string;
  oldParentId: string | null;
  newParentId: string | null;
  timestamp: string;
}

/**
 * Payload for ArticleAssignmentChanged event
 * Sent when an article's assigned user changes
 */
export interface ArticleAssignmentChangedPayload {
  articleId: string;
  userId: string | null;
  userName: string | null;
  userInitials: string | null;
  userColor: string | null;
  timestamp: string;
}

/**
 * Payload for VersionCreated event
 * Sent when a new version is created
 */
export interface VersionCreatedPayload {
  articleId: string;
  versionId: string;
  versionNumber: string;
  createdAt: string;
}

/**
 * Payload for PlanGenerated event
 * Sent when an improvement plan is generated
 */
export interface PlanGeneratedPayload {
  articleId: string;
  planId: string;
  timestamp: string;
}

/**
 * Payload for ArticleVersionCreated event
 * Sent when a new article version is created
 */
export interface ArticleVersionCreatedPayload {
  articleId: string;
  versionId: string;
  versionNumber: string;
  timestamp: string;
}

/**
 * Payload for ChatTurnStarted event
 * Sent when a chat turn starts
 */
export interface ChatTurnStartedPayload {
  conversationId: string;
  articleId: string;
}

/**
 * Payload for ChatTurnComplete event
 * Sent when a chat turn completes
 */
export interface ChatTurnCompletePayload {
  conversationId: string;
  articleId: string;
}

/**
 * Payload for ChatMessageStreaming event
 * Sent for each text chunk as the AI generates a response
 */
export interface ChatMessageStreamingPayload {
  conversationId: string;
  articleId: string;
  text: string | null;
  toolName: string | null;
  toolCallId: string | null;
  toolDisplay: string | null;
  toolResultIds: string[] | null;
  isError: boolean | null;
  messageId: string | null;
  timestamp: string;
}

/**
 * Payload for ChatToolInvoked event
 * Sent when the AI agent invokes a tool/function
 */
export interface ChatToolInvokedPayload {
  conversationId: string;
  articleId: string;
  toolName: string;
  toolCallId: string;
  toolDisplay: string | null;
  timestamp: string;
}

/**
 * Payload for ChatToolCompleted event
 * Sent when a tool invocation completes
 */
export interface ChatToolCompletedPayload {
  conversationId: string;
  articleId: string;
  toolCallId: string;
  toolResultIds: string[] | null;
  timestamp: string;
}

/**
 * Payload for ChatMessageComplete event
 * Sent when the AI response is complete and saved to database
 */
export interface ChatMessageCompletePayload {
  id: string;
  conversationId: string;
  articleId: string;
  content: string;
  timestamp: string;
}

/**
 * Payload for ChatError event
 * Sent when an error occurs during chat processing
 */
export interface ChatErrorPayload {
  conversationId: string;
  articleId: string;
  message: string;
  timestamp: string;
}

/**
 * Payload for ReceiveMessage event
 * Sent when a message is received in an article's chat room
 */
export interface ReceiveMessagePayload {
  articleId: string;
  userName: string;
  message: string;
  timestamp: string;
}

/**
 * Server-to-client methods interface for ArticleHub
 * Defines all methods that the server can call on connected clients
 */
export interface ArticleHubServerMethods {
  ArticleCreated: (payload: ArticleCreatedPayload) => void;
  ArticleUpdated: (payload: ArticleUpdatedPayload) => void;
  ArticleDeleted: (payload: ArticleDeletedPayload) => void;
  ArticleMoved: (payload: ArticleMovedPayload) => void;
  ArticleAssignmentChanged: (payload: ArticleAssignmentChangedPayload) => void;
  VersionCreated: (payload: VersionCreatedPayload) => void;
  PlanGenerated: (payload: PlanGeneratedPayload) => void;
  ArticleVersionCreated: (payload: ArticleVersionCreatedPayload) => void;
  ChatTurnStarted: (payload: ChatTurnStartedPayload) => void;
  ChatTurnComplete: (payload: ChatTurnCompletePayload) => void;
  ChatMessageStreaming: (payload: ChatMessageStreamingPayload) => void;
  ChatToolInvoked: (payload: ChatToolInvokedPayload) => void;
  ChatToolCompleted: (payload: ChatToolCompletedPayload) => void;
  ChatMessageComplete: (payload: ChatMessageCompletePayload) => void;
  ChatError: (payload: ChatErrorPayload) => void;
  ReceiveMessage: (payload: ReceiveMessagePayload) => void;
}

/**
 * Typed hub connection for ArticleHub
 * Provides compile-time type safety for event handlers
 */
export type ArticleHubConnection = HubConnection & {
  on<K extends keyof ArticleHubServerMethods>(
    methodName: K,
    handler: ArticleHubServerMethods[K]
  ): void;
};
