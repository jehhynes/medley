import type { ArticleSummaryDto, ArticleTypeDto } from '@/types/generated/api-client';

/**
 * Parent path item for breadcrumb navigation
 */
interface ParentPathItem {
  id: string;
  title: string;
}

/**
 * State structure for article tree management
 */
interface ArticleTreeState {
  articles: {
    list: ArticleSummaryDto[];
    index: Map<string, ArticleSummaryDto>;
    parentPathCache: Map<string, ParentPathItem[]>;
    typeIndexMap: Record<string, ArticleTypeDto>;
    expandedIds: Set<string>;
    selectedId: string | null;
    selected: ArticleSummaryDto | null;
  };
  editor: {
    title: string;
    content: string;
  };
}

/**
 * Return type for useArticleTree composable
 */
interface UseArticleTreeReturn {
  buildArticleIndex: (articles?: ArticleSummaryDto[]) => void;
  buildParentPathCache: (articles?: ArticleSummaryDto[], path?: ParentPathItem[]) => void;
  sortArticles: (articles: ArticleSummaryDto[]) => void;
  sortArticlesRecursive: (articles: ArticleSummaryDto[]) => void;
  getArticleParents: (articleId: string) => string[];
  findParentArray: (articleId: string, articles?: ArticleSummaryDto[]) => ArticleSummaryDto[] | null;
  insertArticleIntoTree: (article: ArticleSummaryDto) => void;
  updateArticleInTree: (articleId: string, updates: Partial<ArticleSummaryDto>) => void;
  removeArticleFromTree: (articleId: string) => void;
  moveArticleInTree: (articleId: string, oldParentId: string | null, newParentId: string | null) => void;
}

/**
 * Composable for article tree management operations.
 * Provides methods for building indexes, managing tree structure, sorting, and navigation.
 * 
 * @param state - Reactive state object containing articles list, indexes, and caches
 * @returns Tree management methods
 */
export function useArticleTree(state: ArticleTreeState): UseArticleTreeReturn {
  /**
   * Build an index map of all articles in the tree for O(1) lookup.
   * Traverses the tree recursively and stores each article by ID.
   * 
   * @param articles - Array of article objects (defaults to state.articles.list)
   */
  const buildArticleIndex = (articles: ArticleSummaryDto[] = state.articles.list): void => {
    // Clear existing index instead of replacing it to maintain reactivity
    state.articles.index.clear();
    
    const traverse = (items: ArticleSummaryDto[]): void => {
      items.forEach(article => {
        if (article.id) {
          state.articles.index.set(article.id, article);
        }
        if (article.children && article.children.length > 0) {
          traverse(article.children);
        }
      });
    };
    
    traverse(articles);
  };

  /**
   * Build cache for parent paths for all articles.
   * This enables O(1) lookup of article ancestry for tree navigation.
   * 
   * @param articles - Array of article objects (defaults to state.articles.list)
   * @param path - Current path of parent articles (used in recursion)
   */
  const buildParentPathCache = (
    articles: ArticleSummaryDto[] = state.articles.list,
    path: ParentPathItem[] = []
  ): void => {
    // Clear cache when building from root to avoid stale entries
    if (path.length === 0) {
      state.articles.parentPathCache.clear();
    }
    
    articles.forEach(article => {
      if (article.id) {
        state.articles.parentPathCache.set(article.id, [...path]);

        if (article.children && article.children.length > 0) {
          buildParentPathCache(article.children, [
            ...path,
            { id: article.id, title: article.title || '' }
          ]);
        }
      }
    });
  };

  /**
   * Sort articles array in place.
   * Articles of type "Index" are sorted first, then alphabetically by title.
   * 
   * @param articles - Array of articles to sort
   */
  const sortArticles = (articles: ArticleSummaryDto[]): void => {
    articles.sort((a, b) => {
      const aType = a.articleTypeId ? state.articles.typeIndexMap[a.articleTypeId] : undefined;
      const bType = b.articleTypeId ? state.articles.typeIndexMap[b.articleTypeId] : undefined;

      const aIsIndex = aType?.name?.toLowerCase() === 'index';
      const bIsIndex = bType?.name?.toLowerCase() === 'index';

      if (aIsIndex && !bIsIndex) return -1;
      if (!aIsIndex && bIsIndex) return 1;

      const aTitle = a.title || '';
      const bTitle = b.title || '';
      return aTitle.localeCompare(bTitle, undefined, { sensitivity: 'base' });
    });
  };

  /**
   * Sort articles recursively throughout the entire tree.
   * 
   * @param articles - Root array of articles to sort
   */
  const sortArticlesRecursive = (articles: ArticleSummaryDto[]): void => {
    sortArticles(articles);

    articles.forEach(article => {
      if (article.children && article.children.length > 0) {
        sortArticlesRecursive(article.children);
      }
    });
  };

  /**
   * Get the parent path for an article as an array of parent IDs.
   * 
   * @param articleId - ID of the article
   * @returns Array of parent article IDs
   */
  const getArticleParents = (articleId: string): string[] => {
    const parentPath = state.articles.parentPathCache.get(articleId);
    return parentPath ? parentPath.map(p => p.id) : [];
  };

  /**
   * Find the array that contains an article (either root or parent's children array).
   * Used for surgical tree modifications.
   * 
   * @param articleId - ID of article to find
   * @param articles - Array to search in (defaults to state.articles.list)
   * @returns The array containing the article, or null if not found
   */
  const findParentArray = (
    articleId: string,
    articles: ArticleSummaryDto[] = state.articles.list
  ): ArticleSummaryDto[] | null => {
    for (const article of articles) {
      if (article.id === articleId) {
        return articles;
      }
    }

    for (const article of articles) {
      if (article.children && article.children.length > 0) {
        const found = findParentArray(articleId, article.children);
        if (found) {
          return found;
        }
      }
    }

    return null;
  };

  /**
   * Insert a new article into the tree at the appropriate location.
   * Updates indexes, caches, and sorts the parent array.
   * 
   * @param article - Article to insert
   */
  const insertArticleIntoTree = (article: ArticleSummaryDto): void => {
    if (!article.id) {
      console.warn('Cannot insert article without ID');
      return;
    }

    const existing = state.articles.index.get(article.id);
    if (existing) {
      return;
    }

    if (!article.children) {
      article.children = [];
    }

    if (!article.parentArticleId) {
      state.articles.list.push(article);
      sortArticles(state.articles.list);
      state.articles.parentPathCache.set(article.id, []);
    } else {
      const parent = state.articles.index.get(article.parentArticleId);
      if (parent) {
        if (!parent.children) {
          parent.children = [];
        }
        parent.children.push(article);
        sortArticles(parent.children);
        state.articles.expandedIds.add(parent.id!);

        const parentPath = state.articles.parentPathCache.get(parent.id!) || [];
        const newPath: ParentPathItem[] = [
          ...parentPath,
          { id: parent.id!, title: parent.title || '' }
        ];
        state.articles.parentPathCache.set(article.id, newPath);
      } else {
        console.warn(`Parent article ${article.parentArticleId} not found, inserting at root`);
        state.articles.list.push(article);
        sortArticles(state.articles.list);
        state.articles.parentPathCache.set(article.id, []);
      }
    }

    state.articles.index.set(article.id, article);
  };

  /**
   * Update an article's properties in the tree.
   * Handles title changes which require updating child breadcrumbs and re-sorting.
   * 
   * @param articleId - ID of article to update
   * @param updates - Object containing properties to update
   */
  const updateArticleInTree = (articleId: string, updates: Partial<ArticleDto>): void => {
    const article = state.articles.index.get(articleId);
    if (article) {
      Object.assign(article, updates);

      if (updates.title !== undefined && article.children && article.children.length > 0) {
        buildParentPathCache(article.children, [
          ...(state.articles.parentPathCache.get(articleId) || []),
          { id: article.id!, title: article.title || '' }
        ]);
      }

      if (updates.title !== undefined || updates.articleTypeId !== undefined) {
        const parentArray = findParentArray(articleId);
        if (parentArray) {
          sortArticles(parentArray);
        }
      }

      if (state.articles.selectedId === articleId) {
        if (updates.title !== undefined) {
          state.editor.title = updates.title;
        }
        if (updates.content !== undefined) {
          state.editor.content = updates.content || '';
        }
        if (state.articles.selected) {
          Object.assign(state.articles.selected, updates);
        }
      }
    } else {
      console.warn(`Article ${articleId} not found in tree for update`);
    }
  };

  /**
   * Remove an article and all its children from the tree.
   * Cleans up all indexes and caches.
   * 
   * @param articleId - ID of article to remove
   */
  const removeArticleFromTree = (articleId: string): void => {
    const article = state.articles.index.get(articleId);

    const removeFromArray = (articles: ArticleSummaryDto[]): boolean => {
      for (let i = 0; i < articles.length; i++) {
        if (articles[i].id === articleId) {
          articles.splice(i, 1);
          return true;
        }
        if (articles[i].children && articles[i].children!.length > 0) {
          if (removeFromArray(articles[i].children!)) {
            return true;
          }
        }
      }
      return false;
    };

    removeFromArray(state.articles.list);

    const removeFromCaches = (art: ArticleSummaryDto | undefined): void => {
      if (!art || !art.id) return;
      state.articles.index.delete(art.id);
      state.articles.parentPathCache.delete(art.id);
      if (art.children && art.children.length > 0) {
        art.children.forEach(child => removeFromCaches(child));
      }
    };

    removeFromCaches(article);
  };

  /**
   * Move an article from one parent to another in the tree.
   * Updates all caches and sorts affected arrays.
   * 
   * @param articleId - ID of article to move
   * @param oldParentId - ID of old parent (unused but kept for API consistency)
   * @param newParentId - ID of new parent
   */
  const moveArticleInTree = (
    articleId: string,
    oldParentId: string | null,
    newParentId: string | null
  ): void => {
    let movedArticle: ArticleSummaryDto | null = null;

    const removeFromParent = (articles: ArticleSummaryDto[]): boolean => {
      for (let i = 0; i < articles.length; i++) {
        if (articles[i].id === articleId) {
          movedArticle = articles.splice(i, 1)[0];
          return true;
        }
        if (articles[i].children && articles[i].children!.length > 0) {
          if (removeFromParent(articles[i].children!)) {
            return true;
          }
        }
      }
      return false;
    };

    removeFromParent(state.articles.list);

    if (!movedArticle) {
      console.error('Article not found for move:', articleId);
      return;
    }

    if (newParentId) {
      const newParent = state.articles.index.get(newParentId);
      if (newParent) {
        if (!newParent.children) {
          newParent.children = [];
        }
        newParent.children.push(movedArticle);
        sortArticles(newParent.children);

        const parentPath = state.articles.parentPathCache.get(newParentId) || [];
        const newPath: ParentPathItem[] = [
          ...parentPath,
          { id: newParent.id!, title: newParent.title || '' }
        ];
        buildParentPathCache([movedArticle], newPath);

        state.articles.expandedIds.add(newParentId);
      } else {
        console.error('New parent not found:', newParentId);
        state.articles.list.push(movedArticle);
        sortArticles(state.articles.list);
        buildParentPathCache([movedArticle], []);
      }
    } else {
      // Move to root
      state.articles.list.push(movedArticle);
      sortArticles(state.articles.list);
      buildParentPathCache([movedArticle], []);
    }
  };

  return {
    buildArticleIndex,
    buildParentPathCache,
    sortArticles,
    sortArticlesRecursive,
    getArticleParents,
    findParentArray,
    insertArticleIntoTree,
    updateArticleInTree,
    removeArticleFromTree,
    moveArticleInTree
  };
}
