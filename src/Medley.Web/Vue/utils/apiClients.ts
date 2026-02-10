import {
  ArticlesApiClient,
  ArticleChatApiClient,
  ArticleReviewsApiClient,
  DashboardApiClient,
  FragmentsApiClient,
  KnowledgeUnitsApiClient,
  PlanApiClient,
  SourcesApiClient,
  TagTypesApiClient,
  AiPromptApiClient,
  SpeakersApiClient,
  TokenUsageApiClient,
  UsersApiClient
} from '@/types/api-client';

export const apiClients = {
  articles: new ArticlesApiClient(),
  articleChat: new ArticleChatApiClient(),
  articleReviews: new ArticleReviewsApiClient(),
  dashboard: new DashboardApiClient(),
  fragments: new FragmentsApiClient(),
  knowledgeUnits: new KnowledgeUnitsApiClient(),
  plans: new PlanApiClient(),
  sources: new SourcesApiClient(),
  tagTypes: new TagTypesApiClient(),
  aiPrompts: new AiPromptApiClient(),
  speakers: new SpeakersApiClient(),
  tokenUsage: new TokenUsageApiClient(),
  users: new UsersApiClient()
} as const;

export const {
  articles: articlesClient,
  articleChat: articleChatClient,
  articleReviews: articleReviewsClient,
  dashboard: dashboardClient,
  fragments: fragmentsClient,
  knowledgeUnits: knowledgeUnitsClient,
  plans: plansClient,
  sources: sourcesClient,
  tagTypes: tagTypesClient,
  aiPrompts: aiPromptsClient,
  speakers: speakersClient,
  tokenUsage: tokenUsageClient,
  users: usersClient
} = apiClients;
