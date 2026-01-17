/**
 * Type guard functions for runtime validation of DTOs
 * These functions validate that unknown data matches expected TypeScript interfaces
 */

import type {
  ArticleDto,
  UserSummaryDto,
  FragmentDto,
  ConversationSummaryDto
} from './generated/api-client';

/**
 * Type guard for UserSummaryDto
 * Validates that a value matches the UserSummaryDto interface
 */
export function isUserSummary(value: unknown): value is UserSummaryDto {
  if (typeof value !== 'object' || value === null) {
    return false;
  }

  const obj = value as Record<string, unknown>;

  return (
    typeof obj.id === 'string' &&
    typeof obj.fullName === 'string' &&
    (obj.initials === undefined || typeof obj.initials === 'string') &&
    (obj.color === undefined || typeof obj.color === 'string')
  );
}

/**
 * Type guard for ConversationSummaryDto
 * Validates that a value matches the ConversationSummaryDto interface
 */
export function isConversationSummary(value: unknown): value is ConversationSummaryDto {
  if (typeof value !== 'object' || value === null) {
    return false;
  }

  const obj = value as Record<string, unknown>;

  return (
    typeof obj.id === 'string' &&
    (obj.isRunning === undefined || typeof obj.isRunning === 'boolean') &&
    (obj.state === undefined || typeof obj.state === 'string') &&
    (obj.mode === undefined || typeof obj.mode === 'string') &&
    (obj.createdAt === undefined || obj.createdAt instanceof Date || typeof obj.createdAt === 'string') &&
    (obj.lastMessageAt === undefined || obj.lastMessageAt instanceof Date || typeof obj.lastMessageAt === 'string') &&
    (obj.messageCount === undefined || typeof obj.messageCount === 'number') &&
    (obj.planId === undefined || obj.planId === null || typeof obj.planId === 'string')
  );
}

/**
 * Type guard for ArticleDto
 * Validates that a value matches the ArticleDto interface
 */
export function isArticle(value: unknown): value is ArticleDto {
  if (typeof value !== 'object' || value === null) {
    return false;
  }

  const obj = value as Record<string, unknown>;

  // Check required fields
  if (typeof obj.id !== 'string' || typeof obj.title !== 'string') {
    return false;
  }

  // Check optional fields with proper type validation
  if (obj.summary !== undefined && obj.summary !== null && typeof obj.summary !== 'string') {
    return false;
  }

  if (obj.content !== undefined && obj.content !== null && typeof obj.content !== 'string') {
    return false;
  }

  if (obj.status !== undefined && typeof obj.status !== 'string') {
    return false;
  }

  if (obj.publishedAt !== undefined && obj.publishedAt !== null && 
      !(obj.publishedAt instanceof Date) && typeof obj.publishedAt !== 'string') {
    return false;
  }

  if (obj.parentArticleId !== undefined && obj.parentArticleId !== null && 
      typeof obj.parentArticleId !== 'string') {
    return false;
  }

  if (obj.articleTypeId !== undefined && obj.articleTypeId !== null && 
      typeof obj.articleTypeId !== 'string') {
    return false;
  }

  if (obj.articleTypeName !== undefined && obj.articleTypeName !== null && 
      typeof obj.articleTypeName !== 'string') {
    return false;
  }

  // Validate nested objects
  if (obj.assignedUser !== undefined && obj.assignedUser !== null && 
      !isUserSummary(obj.assignedUser)) {
    return false;
  }

  if (obj.currentConversation !== undefined && obj.currentConversation !== null && 
      !isConversationSummary(obj.currentConversation)) {
    return false;
  }

  // Validate arrays
  if (obj.children !== undefined && !Array.isArray(obj.children)) {
    return false;
  }

  // Validate date fields
  if (obj.createdAt !== undefined && 
      !(obj.createdAt instanceof Date) && typeof obj.createdAt !== 'string') {
    return false;
  }

  if (obj.updatedAt !== undefined && 
      !(obj.updatedAt instanceof Date) && typeof obj.updatedAt !== 'string') {
    return false;
  }

  if (obj.currentVersionNumber !== undefined && obj.currentVersionNumber !== null && 
      typeof obj.currentVersionNumber !== 'number') {
    return false;
  }

  if (obj.currentVersionId !== undefined && obj.currentVersionId !== null && 
      typeof obj.currentVersionId !== 'string') {
    return false;
  }

  return true;
}

/**
 * Type guard for FragmentDto
 * Validates that a value matches the FragmentDto interface
 */
export function isFragment(value: unknown): value is FragmentDto {
  if (typeof value !== 'object' || value === null) {
    return false;
  }

  const obj = value as Record<string, unknown>;

  // Check required fields
  if (typeof obj.id !== 'string') {
    return false;
  }

  // Check optional fields
  if (obj.title !== undefined && obj.title !== null && typeof obj.title !== 'string') {
    return false;
  }

  if (obj.summary !== undefined && obj.summary !== null && typeof obj.summary !== 'string') {
    return false;
  }

  if (obj.text !== undefined && obj.text !== null && typeof obj.text !== 'string') {
    return false;
  }

  if (obj.category !== undefined && obj.category !== null && typeof obj.category !== 'string') {
    return false;
  }

  if (obj.confidence !== undefined && obj.confidence !== null && typeof obj.confidence !== 'string') {
    return false;
  }

  if (obj.sourceId !== undefined && obj.sourceId !== null && typeof obj.sourceId !== 'string') {
    return false;
  }

  if (obj.sourceName !== undefined && obj.sourceName !== null && typeof obj.sourceName !== 'string') {
    return false;
  }

  if (obj.sourceType !== undefined && obj.sourceType !== null && typeof obj.sourceType !== 'string') {
    return false;
  }

  if (obj.sourceDate !== undefined && obj.sourceDate !== null && 
      !(obj.sourceDate instanceof Date) && typeof obj.sourceDate !== 'string') {
    return false;
  }

  if (obj.createdAt !== undefined && 
      !(obj.createdAt instanceof Date) && typeof obj.createdAt !== 'string') {
    return false;
  }

  if (obj.updatedAt !== undefined && 
      !(obj.updatedAt instanceof Date) && typeof obj.updatedAt !== 'string') {
    return false;
  }

  if (obj.similarity !== undefined && obj.similarity !== null && typeof obj.similarity !== 'number') {
    return false;
  }

  if (obj.isUsed !== undefined && obj.isUsed !== null && typeof obj.isUsed !== 'boolean') {
    return false;
  }

  if (obj.usedInArticleIds !== undefined && !Array.isArray(obj.usedInArticleIds)) {
    return false;
  }

  return true;
}

/**
 * Type guard for chat message structure
 * Note: ChatMessage is not in the generated API client, so we define a minimal interface
 */
export interface ChatMessage {
  id: string;
  role: string;
  content: string;
  timestamp: Date | string;
}

export function isChatMessage(value: unknown): value is ChatMessage {
  if (typeof value !== 'object' || value === null) {
    return false;
  }

  const obj = value as Record<string, unknown>;

  return (
    typeof obj.id === 'string' &&
    typeof obj.role === 'string' &&
    typeof obj.content === 'string' &&
    (obj.timestamp instanceof Date || typeof obj.timestamp === 'string')
  );
}
