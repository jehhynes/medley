/**
 * SignalR Hub Type Definitions
 * 
 * This module exports strongly-typed interfaces for SignalR hub connections,
 * providing compile-time type safety for real-time messaging.
 */

// ArticleHub types
export type {
  ArticleCreatedPayload,
  ArticleUpdatedPayload,
  ArticleDeletedPayload,
  ArticleMovedPayload,
  ArticleAssignmentChangedPayload,
  VersionCreatedPayload,
  PlanGeneratedPayload,
  ArticleVersionCreatedPayload,
  ChatTurnStartedPayload,
  ChatTurnCompletePayload,
  ChatMessageStreamingPayload,
  ChatToolInvokedPayload,
  ChatToolCompletedPayload,
  ChatMessageCompletePayload,
  ChatErrorPayload,
  ReceiveMessagePayload,
  ArticleHubServerMethods,
  ArticleHubConnection
} from './article-hub';

// AdminHub types
export type {
  IntegrationStatusUpdatePayload,
  FragmentExtractionCompletePayload,
  AdminHubServerMethods,
  AdminHubConnection
} from './admin-hub';
