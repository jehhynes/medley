import type { HubConnection } from '@microsoft/signalr';

export interface IntegrationStatusUpdatePayload {
  integrationId: string;
  status: string;
  message: string;
}

export interface FragmentExtractionCompletePayload {
  sourceId: string;
  fragmentCount: number;
  success: boolean;
  message: string;
}

export interface AdminHubServerMethods {
  IntegrationStatusUpdate: (payload: IntegrationStatusUpdatePayload) => void;
  FragmentExtractionComplete: (payload: FragmentExtractionCompletePayload) => void;
}

/** Typed hub connection with compile-time type safety for event handlers */
export type AdminHubConnection = HubConnection & {
  on<K extends keyof AdminHubServerMethods>(
    methodName: K,
    handler: AdminHubServerMethods[K]
  ): void;
};
