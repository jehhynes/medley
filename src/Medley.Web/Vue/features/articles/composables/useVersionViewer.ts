import { type ComputedRef, type Ref } from 'vue';
import type { ArticleVersionDto } from '@/types/api-client';

export interface UseVersionViewerOptions {
  openVersionTab?: (version: ArticleVersionDto) => void;
  switchContentTab?: (tab: string) => void;
  selectedArticleId?: string | null | ComputedRef<string | null> | Ref<string | null>;
}

/** Composable for article version history and diff viewing */
export function useVersionViewer(options: UseVersionViewerOptions = {}) {
  const handleVersionSelect = async (version: ArticleVersionDto | null) => {
    const articleId = typeof options.selectedArticleId === 'object' && options.selectedArticleId !== null && 'value' in options.selectedArticleId
      ? options.selectedArticleId.value
      : options.selectedArticleId;
    
    if (!version || !articleId) return;

    if (options.openVersionTab) {
      options.openVersionTab(version);
    }
  };

  const markdownToHtml = (markdown: string | null | undefined) => {
    if (!markdown) return '';

    const marked = (window as any).marked;
    if (marked) {
      try {
        return marked.parse(markdown, {
          breaks: true,
          gfm: true,
          headerIds: false,
          mangle: false
        });
      } catch (e) {
        console.error('Failed to parse markdown:', e);
        return markdown;
      }
    }

    return `<pre>${markdown}</pre>`;
  };

  const clearVersionSelection = () => {
    if (options.switchContentTab) {
      options.switchContentTab('editor');
    }
  };

  return {
    handleVersionSelect,
    markdownToHtml,
    clearVersionSelection
  };
}
