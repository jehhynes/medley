import { computed } from 'vue';
import { showProcessingSpinner, showUserTurnIndicator } from '@/utils/helpers.js';

/**
 * Composable for "My Work" filtering and sorting logic.
 * Shared between MyWorkList component and Articles parent for badge count.
 * 
 * @param {Object} articles - Reactive articles list (tree structure)
 * @param {string} currentUserId - Current user ID
 * @returns {Object} Computed myWorkArticles and myWorkCount
 */
export function useMyWork(articles, currentUserId) {
  /**
   * Flatten all articles in the tree
   * @param {Array} articles - Articles to flatten
   * @returns {Array} Flattened articles
   */
  const flattenAllArticles = (articles) => {
    let result = [];
    for (const article of articles) {
      result.push({ ...article });
      if (article.children && article.children.length > 0) {
        result = result.concat(flattenAllArticles(article.children));
      }
    }
    return result;
  };

  /**
   * Get the last activity date for an article
   * @param {Object} article - Article to get activity date for
   * @returns {Date} Latest activity date
   */
  const getLastActivityDate = (article) => {
    let latestDate = new Date(article.modifiedAt || article.createdAt || 0);
    
    // Check conversation's last message time if available
    if (article.currentConversation?.lastMessageAt) {
      const conversationDate = new Date(article.currentConversation.lastMessageAt);
      if (conversationDate > latestDate) {
        latestDate = conversationDate;
      }
    }
    
    // Check latest version date if available
    if (article.latestVersionDate) {
      const versionDate = new Date(article.latestVersionDate);
      if (versionDate > latestDate) {
        latestDate = versionDate;
      }
    }
    
    return latestDate;
  };

  /**
   * Filter and sort articles for "My Work" view
   */
  const myWorkArticles = computed(() => {
    if (!currentUserId.value) {
      return [];
    }

    // Flatten all articles
    const allArticles = flattenAllArticles(articles.value);
    
    // Filter: assigned to user AND status is Draft or Review
    const filtered = allArticles.filter(article => {
      // Check if assigned to user
      const isAssigned = article.assignedUser && 
                        article.assignedUser.id && 
                        article.assignedUser.id === currentUserId.value;
      
      // Check if status is Draft or Review (status comes as string from API)
      const isDraftOrReview = article.status === 'Draft' || article.status === 'Review';
      
      return isAssigned && isDraftOrReview;
    });
    
    // Sort by priority
    return filtered.sort((a, b) => {
      // Priority 1: User's turn (waiting for review)
      const aUserTurn = showUserTurnIndicator(a) ? 1 : 0;
      const bUserTurn = showUserTurnIndicator(b) ? 1 : 0;
      if (aUserTurn !== bUserTurn) return bUserTurn - aUserTurn;
      
      // Priority 2: No conversation (assigned but no conversation yet)
      const aNoConversation = (!a.currentConversation || !a.currentConversation.id) ? 1 : 0;
      const bNoConversation = (!b.currentConversation || !b.currentConversation.id) ? 1 : 0;
      if (aNoConversation !== bNoConversation) return bNoConversation - aNoConversation;
      
      // Priority 3: AI processing
      const aAiProcessing = showProcessingSpinner(a) ? 1 : 0;
      const bAiProcessing = showProcessingSpinner(b) ? 1 : 0;
      if (aAiProcessing !== bAiProcessing) return bAiProcessing - aAiProcessing;
      
      // Priority 4: Last activity date (most recent first)
      const aLastActivity = getLastActivityDate(a);
      const bLastActivity = getLastActivityDate(b);
      return bLastActivity - aLastActivity;
    });
  });

  /**
   * Count of articles in "My Work"
   */
  const myWorkCount = computed(() => {
    return myWorkArticles.value.length;
  });

  return {
    myWorkArticles,
    myWorkCount,
    getLastActivityDate
  };
}
