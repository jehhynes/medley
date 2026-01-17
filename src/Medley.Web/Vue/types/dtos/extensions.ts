// Manual type extensions for SignalR-specific types and other types not in REST API
// This file extends the auto-generated API types with additional types needed for SignalR
// and other frontend-specific use cases that are not part of the REST API

// Re-export all generated types for convenience
export * from '../generated/api-client';

// Additional types for SignalR payloads (not part of REST API)

/**
 * Conversation summary for SignalR updates
 * This is used in real-time updates but not exposed in the REST API
 */
export interface ConversationSummary {
  id: string;
  isRunning: boolean;
}

/**
 * Enum for stream update types used in SignalR chat streaming
 * These types indicate the kind of update being sent during a chat stream
 */
export enum StreamUpdateType {
  TextDelta = 'TextDelta',
  ToolCall = 'ToolCall',
  ToolResult = 'ToolResult',
  MessageComplete = 'MessageComplete',
  TurnComplete = 'TurnComplete'
}

/**
 * Chat stream update payload for SignalR streaming messages
 * This is sent during real-time chat streaming and is not part of the REST API
 */
export interface ChatStreamUpdate {
  /** The type of stream update */
  type: StreamUpdateType;
  
  /** The conversation ID this update belongs to */
  conversationId: string;
  
  /** The article ID associated with this conversation (if any) */
  articleId: string | null;
  
  /** Text content for TextDelta updates */
  text: string | null;
  
  /** Tool name for ToolCall updates */
  toolName: string | null;
  
  /** Tool call ID for ToolCall and ToolResult updates */
  toolCallId: string | null;
  
  /** Display text for tool calls */
  toolDisplay: string | null;
  
  /** Array of tool result IDs for ToolResult updates */
  toolResultIds: string[] | null;
  
  /** Indicates if this is an error message */
  isError: boolean | null;
  
  /** Message ID for MessageComplete updates */
  messageId: string | null;
  
  /** Timestamp of the update */
  timestamp: string;
}
