import {
  ArticlesApiClient,
  ArticleChatApiClient,
  DashboardApiClient,
  FragmentsApiClient,
  PlanApiClient,
  SourcesApiClient,
  TagTypesApiClient,
  AiPromptApiClient
} from '@/types/api-client';

export const apiClients = {
  articles: new ArticlesApiClient(),
  articleChat: new ArticleChatApiClient(),
  dashboard: new DashboardApiClient(),
  fragments: new FragmentsApiClient(),
  plans: new PlanApiClient(),
  sources: new SourcesApiClient(),
  tagTypes: new TagTypesApiClient(),
  aiPrompts: new AiPromptApiClient()
} as const;

export const {
  articles: articlesClient,
  articleChat: articleChatClient,
  dashboard: dashboardClient,
  fragments: fragmentsClient,
  plans: plansClient,
  sources: sourcesClient,
  tagTypes: tagTypesClient,
  aiPrompts: aiPromptsClient
} = apiClients;
