import { computed, type Ref, type ComputedRef } from 'vue';
import type { ArticleSummaryDto } from '@/types/api-client';
import { showProcessingSpinner, showUserTurnIndicator } from '@/utils/helpers';

interface UseMyWorkReturn {
  myWorkArticles: ComputedRef<ArticleSummaryDto[]>;
  myWorkCount: ComputedRef<number>;
  getLastActivityDate: (article: ArticleSummaryDto) => Date;
}

/** Filters and sorts articles for "My Work" view based on assignment and status */
export function useMyWork(
  articles: Ref<ArticleSummaryDto[]>,
  currentUserId: Ref<string | null | undefined>
): UseMyWorkReturn {
  const flattenAllArticles = (articles: ArticleSummaryDto[]): ArticleSummaryDto[] => {
    let result: ArticleSummaryDto[] = [];
    for (const article of articles) {
      result.push({ ...article });
      if (article.children && article.children.length > 0) {
        result = result.concat(flattenAllArticles(article.children));
      }
    }
    return result;
  };

  const getLastActivityDate = (article: ArticleSummaryDto): Date => {
    let latestDate = new Date(article.modifiedAt || article.createdAt || 0);
    
    if (article.currentConversation?.isRunning) {
      const now = new Date();
      if (now > latestDate) {
        latestDate = now;
      }
    }
    
    return latestDate;
  };

  const myWorkArticles = computed(() => {
    if (!currentUserId.value) {
      return [];
    }

    const allArticles = flattenAllArticles(articles.value);
    
    const filtered = allArticles.filter(article => {
      const isAssigned = article.assignedUser && 
                        article.assignedUser.id && 
                        article.assignedUser.id === currentUserId.value;
      
      const isDraftOrReview = article.status === 'Draft' || article.status === 'Review';
      
      return isAssigned && isDraftOrReview;
    });
    
    return filtered.sort((a, b) => {
      const aUserTurn = showUserTurnIndicator(a) ? 1 : 0;
      const bUserTurn = showUserTurnIndicator(b) ? 1 : 0;
      if (aUserTurn !== bUserTurn) return bUserTurn - aUserTurn;
      
      const aNoConversation = (!a.currentConversation || !a.currentConversation.id) ? 1 : 0;
      const bNoConversation = (!b.currentConversation || !b.currentConversation.id) ? 1 : 0;
      if (aNoConversation !== bNoConversation) return bNoConversation - aNoConversation;
      
      const aAiProcessing = showProcessingSpinner(a) ? 1 : 0;
      const bAiProcessing = showProcessingSpinner(b) ? 1 : 0;
      if (aAiProcessing !== bAiProcessing) return bAiProcessing - aAiProcessing;
      
      const aLastActivity = getLastActivityDate(a);
      const bLastActivity = getLastActivityDate(b);
      return bLastActivity.getTime() - aLastActivity.getTime();
    });
  });

  const myWorkCount = computed(() => {
    return myWorkArticles.value.length;
  });

  return {
    myWorkArticles,
    myWorkCount,
    getLastActivityDate
  };
}
