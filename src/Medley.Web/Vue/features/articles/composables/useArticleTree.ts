import type { ArticleSummaryDto, ArticleTypeDto } from '@/types/api-client';

interface ParentPathItem {
  id: string;
  title: string;
}

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

/** Manages article tree operations: indexing, sorting, and tree structure modifications */
export function useArticleTree(state: ArticleTreeState) {
  const buildArticleIndex = (articles: ArticleSummaryDto[] = state.articles.list) => {
    state.articles.index.clear();
    
    const traverse = (items: ArticleSummaryDto[]) => {
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

  const buildParentPathCache = (
    articles: ArticleSummaryDto[] = state.articles.list,
    path: ParentPathItem[] = []
  ) => {
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

  const sortArticles = (articles: ArticleSummaryDto[]) => {
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

  const sortArticlesRecursive = (articles: ArticleSummaryDto[]) => {
    sortArticles(articles);

    articles.forEach(article => {
      if (article.children && article.children.length > 0) {
        sortArticlesRecursive(article.children);
      }
    });
  };

  const getArticleParents = (articleId: string) => {
    const parentPath = state.articles.parentPathCache.get(articleId);
    return parentPath ? parentPath.map(p => p.id) : [];
  };

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

  const insertArticleIntoTree = (article: ArticleSummaryDto) => {
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

  const updateArticleInTree = (articleId: string, updates: Partial<ArticleDto>) => {
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

  const removeArticleFromTree = (articleId: string) => {
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

    const removeFromCaches = (art: ArticleSummaryDto | undefined) => {
      if (!art || !art.id) return;
      state.articles.index.delete(art.id);
      state.articles.parentPathCache.delete(art.id);
      if (art.children && art.children.length > 0) {
        art.children.forEach(child => removeFromCaches(child));
      }
    };

    removeFromCaches(article);
  };

  const moveArticleInTree = (
    articleId: string,
    oldParentId: string | null,
    newParentId: string | null
  ) => {
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
