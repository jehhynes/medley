/**
 * Centralized API client wrapper providing singleton access to all NSwag-generated clients
 */

import {
  ArticlesApiClient,
  ArticleChatApiClient,
  DashboardApiClient,
  FragmentsApiClient,
  PlanApiClient,
  SourcesApiClient,
  TagTypesApiClient,
  TemplatesApiClient
} from '@/types/api-client';

/**
 * Singleton instances of all API clients
 * Use these throughout the application for type-safe API calls
 * 
 * @example
 * ```typescript
 * import { apiClients } from '@/utils/apiClients';
 * 
 * // Get articles
 * const articles = await apiClients.articles.getTree();
 * 
 * // Get a single article
 * const article = await apiClients.articles.get(articleId);
 * 
 * // Create an article
 * const newArticle = await apiClients.articles.create({
 *   title: 'New Article',
 *   parentArticleId: null,
 *   articleTypeId: typeId
 * });
 * ```
 */
export const apiClients = {
  articles: new ArticlesApiClient(),
  articleChat: new ArticleChatApiClient(),
  dashboard: new DashboardApiClient(),
  fragments: new FragmentsApiClient(),
  plans: new PlanApiClient(),
  sources: new SourcesApiClient(),
  tagTypes: new TagTypesApiClient(),
  templates: new TemplatesApiClient()
} as const;

/**
 * Export individual clients for cases where only one is needed
 */
export const {
  articles: articlesClient,
  articleChat: articleChatClient,
  dashboard: dashboardClient,
  fragments: fragmentsClient,
  plans: plansClient,
  sources: sourcesClient,
  tagTypes: tagTypesClient,
  templates: templatesClient
} = apiClients;
