/**
 * Typed API client wrapper for Medley
 * Provides a simple typed interface for making HTTP requests
 */

// Import only the ApiException class and types (not the duplicate MedleyApiClient classes)
import type {
  ArticleDto,
  ArticleCreateRequest,
  ArticleUpdateMetadataRequest,
  ArticleUpdateContentRequest,
  ArticleMoveRequest,
  ArticleVersionDto,
  ArticleVersionComparisonDto,
  FragmentDto,
  FragmentSearchResult,
  FragmentTitleDto,
  SourceDto,
  SourceSummaryDto,
  ConversationMode,
  SendMessageRequest,
  UpdatePlanRequest,
  UpdatePlanFragmentIncludeRequest,
  UpdatePlanFragmentInstructionsRequest,
  VersionCaptureResponse,
  DashboardMetrics,
  ArticleTypeDto,
  UpdateTemplateRequest,
  ArticleStatus,
  VersionType,
  ReviewAction,
  SourceType,
  ConfidenceLevel,
  ExtractionStatus,
  SourceMetadataType,
  ConversationState,
  UserSummaryDto,
  ConversationSummaryDto
} from '@/types/generated/api-client';

/**
 * API Exception class for typed error handling
 */
export class ApiException extends Error {
  override message: string;
  status: number;
  response: string;
  headers: { [key: string]: any };
  result: any;

  constructor(message: string, status: number, response: string, headers: { [key: string]: any }, result: any) {
    super(message);
    this.message = message;
    this.status = status;
    this.response = response;
    this.headers = headers;
    this.result = result;
  }

  protected isApiException = true;

  static isApiException(obj: any): obj is ApiException {
    return obj.isApiException === true;
  }
}

// Re-export all types for convenience
export type {
  ArticleDto,
  ArticleCreateRequest,
  ArticleUpdateMetadataRequest,
  ArticleUpdateContentRequest,
  ArticleMoveRequest,
  ArticleVersionDto,
  ArticleVersionComparisonDto,
  FragmentDto,
  FragmentSearchResult,
  FragmentTitleDto,
  SourceDto,
  SourceSummaryDto,
  ConversationMode,
  SendMessageRequest,
  UpdatePlanRequest,
  UpdatePlanFragmentIncludeRequest,
  UpdatePlanFragmentInstructionsRequest,
  VersionCaptureResponse,
  DashboardMetrics,
  ArticleTypeDto,
  UpdateTemplateRequest,
  ArticleStatus,
  VersionType,
  ReviewAction,
  SourceType,
  ConfidenceLevel,
  ExtractionStatus,
  SourceMetadataType,
  ConversationState,
  UserSummaryDto,
  ConversationSummaryDto
};

/**
 * Generic typed API client for making HTTP requests
 * Provides type-safe methods for GET, POST, PUT, and DELETE operations
 */
class ApiClient {
  private baseUrl: string;

  constructor(baseUrl?: string) {
    this.baseUrl = baseUrl || window.location.origin;
  }

  /**
   * Make a typed GET request
   * @param url - The URL to request (relative to baseUrl)
   * @returns Promise resolving to the typed response
   */
  async get<T>(url: string): Promise<T> {
    const response = await fetch(`${this.baseUrl}${url}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      const text = await response.text();
      throw new ApiException(
        `API error: ${response.statusText}`,
        response.status,
        text,
        {},
        null
      );
    }

    // Handle 204 No Content responses
    if (response.status === 204) {
      return null as T;
    }

    return response.json();
  }

  /**
   * Make a typed POST request
   * @param url - The URL to request (relative to baseUrl)
   * @param data - The data to send in the request body
   * @returns Promise resolving to the typed response
   */
  async post<TRequest, TResponse>(url: string, data: TRequest): Promise<TResponse> {
    const response = await fetch(`${this.baseUrl}${url}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(data)
    });

    if (!response.ok) {
      const text = await response.text();
      throw new ApiException(
        `API error: ${response.statusText}`,
        response.status,
        text,
        {},
        null
      );
    }

    // Handle 204 No Content responses
    if (response.status === 204) {
      return null as TResponse;
    }

    return response.json();
  }

  /**
   * Make a typed PUT request
   * @param url - The URL to request (relative to baseUrl)
   * @param data - The data to send in the request body
   * @returns Promise resolving to the typed response
   */
  async put<TRequest, TResponse>(url: string, data: TRequest): Promise<TResponse> {
    const response = await fetch(`${this.baseUrl}${url}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(data)
    });

    if (!response.ok) {
      const text = await response.text();
      throw new ApiException(
        `API error: ${response.statusText}`,
        response.status,
        text,
        {},
        null
      );
    }

    // Handle 204 No Content responses
    if (response.status === 204) {
      return null as TResponse;
    }

    return response.json();
  }

  /**
   * Make a DELETE request
   * @param url - The URL to request (relative to baseUrl)
   * @returns Promise resolving to true if successful
   */
  async delete(url: string): Promise<boolean> {
    const response = await fetch(`${this.baseUrl}${url}`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      const text = await response.text();
      throw new ApiException(
        `API error: ${response.statusText}`,
        response.status,
        text,
        {},
        null
      );
    }

    return response.ok;
  }
}

/**
 * Singleton API client instance
 * Use this throughout the application for all API calls
 * 
 * @example
 * ```typescript
 * import { api } from '@/utils/api';
 * import type { ArticleDto } from '@/utils/api';
 * 
 * // GET request
 * const articles = await api.get<ArticleDto[]>('/api/articles');
 * 
 * // POST request
 * const newArticle = await api.post<ArticleCreateRequest, ArticleDto>(
 *   '/api/articles',
 *   { title: 'New Article', parentArticleId: null, articleTypeId: 1 }
 * );
 * 
 * // PUT request
 * const updated = await api.put<ArticleUpdateRequest, ArticleDto>(
 *   `/api/articles/${id}`,
 *   { title: 'Updated Title', content: 'New content' }
 * );
 * 
 * // DELETE request
 * await api.delete(`/api/articles/${id}`);
 * ```
 */
export const api = new ApiClient();

/**
 * Export the client class for testing or custom instances
 */
export { ApiClient };
