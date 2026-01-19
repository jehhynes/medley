import { type ComputedRef, type Ref } from 'vue';
import type { ArticleVersionDto } from '@/types/api-client';

/**
 * Options for useVersionViewer composable
 */
export interface UseVersionViewerOptions {
  /**
   * Function to open a version tab
   */
  openVersionTab?: (version: ArticleVersionDto) => void;

  /**
   * Function to switch content tab
   */
  switchContentTab?: (tab: string) => void;

  /**
   * Current selected article ID (reactive)
   */
  selectedArticleId?: string | null | ComputedRef<string | null> | Ref<string | null>;
}

/**
 * Return type for useVersionViewer composable
 */
interface UseVersionViewerReturn {
  handleVersionSelect: (version: ArticleVersionDto | null) => Promise<void>;
  markdownToHtml: (markdown: string | null | undefined) => string;
  clearVersionSelection: () => void;
}

/**
 * Composable for article version history and diff viewing.
 * Handles version selection, markdown rendering, and version clearing.
 * 
 * @param options - Configuration options for version handling
 * @returns Version management methods
 */
export function useVersionViewer(options: UseVersionViewerOptions = {}): UseVersionViewerReturn {
  /**
   * Handle version selection
   * Opens version in a new tab instead of replacing content
   * 
   * @param version - Version to select
   */
  const handleVersionSelect = async (version: ArticleVersionDto | null): Promise<void> => {
    // Get the actual value if it's a ref/computed
    const articleId = typeof options.selectedArticleId === 'object' && options.selectedArticleId !== null && 'value' in options.selectedArticleId
      ? options.selectedArticleId.value
      : options.selectedArticleId;
    
    if (!version || !articleId) return;

    // Open version in a new tab instead of replacing content
    if (options.openVersionTab) {
      options.openVersionTab(version);
    }
  };

  /**
   * Convert markdown to HTML
   * Uses marked library if available, otherwise returns markdown wrapped in pre tag
   * 
   * @param markdown - Markdown content to convert
   * @returns HTML string
   */
  const markdownToHtml = (markdown: string | null | undefined): string => {
    if (!markdown) return '';

    // Use marked library if available
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

    // Fallback: return markdown as-is wrapped in pre
    return `<pre>${markdown}</pre>`;
  };

  /**
   * Clear version selection and switch back to editor tab
   */
  const clearVersionSelection = (): void => {
    // Switch back to editor tab
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
