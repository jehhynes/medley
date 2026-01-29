import {
  ArticlesApiClient,
  ArticleChatApiClient,
  DashboardApiClient,
  FragmentsApiClient,
  KnowledgeUnitsApiClient,
  PlanApiClient,
  SourcesApiClient,
  TagTypesApiClient,
  AiPromptApiClient,
  SpeakersApiClient,
  TokenUsageApiClient
} from '@/types/api-client';

export const apiClients = {
  articles: new ArticlesApiClient(),
  articleChat: new ArticleChatApiClient(),
  dashboard: new DashboardApiClient(),
  fragments: new FragmentsApiClient(),
  knowledgeUnits: new KnowledgeUnitsApiClient(),
  plans: new PlanApiClient(),
  sources: new SourcesApiClient(),
  tagTypes: new TagTypesApiClient(),
  aiPrompts: new AiPromptApiClient(),
  speakers: new SpeakersApiClient(),
  tokenUsage: new TokenUsageApiClient()
} as const;

export const {
  articles: articlesClient,
  articleChat: articleChatClient,
  dashboard: dashboardClient,
  fragments: fragmentsClient,
  knowledgeUnits: knowledgeUnitsClient,
  plans: plansClient,
  sources: sourcesClient,
  tagTypes: tagTypesClient,
  aiPrompts: aiPromptsClient,
  speakers: speakersClient,
  tokenUsage: tokenUsageClient
} = apiClients;
