import type { HubConnection } from '@microsoft/signalr';

/**
 * Payload for IntegrationStatusUpdate event
 * Sent when an integration's status changes
 */
export interface IntegrationStatusUpdatePayload {
  integrationId: string;
  status: string;
  message: string;
}

/**
 * Payload for FragmentExtractionComplete event
 * Sent when fragment extraction completes for a source
 */
export interface FragmentExtractionCompletePayload {
  sourceId: string;
  fragmentCount: number;
  success: boolean;
  message: string;
}

/**
 * Server-to-client methods interface for AdminHub
 * Defines all methods that the server can call on connected admin clients
 */
export interface AdminHubServerMethods {
  IntegrationStatusUpdate: (payload: IntegrationStatusUpdatePayload) => void;
  FragmentExtractionComplete: (payload: FragmentExtractionCompletePayload) => void;
}

/**
 * Typed hub connection for AdminHub
 * Provides compile-time type safety for event handlers
 */
export type AdminHubConnection = HubConnection & {
  on<K extends keyof AdminHubServerMethods>(
    methodName: K,
    handler: AdminHubServerMethods[K]
  ): void;
};
