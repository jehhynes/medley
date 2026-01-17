/**
 * Version object interface
 */
export interface ArticleVersion {
  id: string;
  versionNumber: number;
  createdAt: string;
  [key: string]: any;
}

/**
 * Options for useArticleVersion composable
 */
export interface UseArticleVersionOptions {
  /**
   * Function to open a version tab
   */
  openVersionTab?: (version: ArticleVersion) => void;

  /**
   * Function to switch content tab
   */
  switchContentTab?: (tab: string) => void;

  /**
   * Current selected article ID
   */
  selectedArticleId?: string | null;
}

/**
 * Return type for useArticleVersion composable
 */
interface UseArticleVersionReturn {
  handleVersionSelect: (version: ArticleVersion | null) => Promise<void>;
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
export function useArticleVersion(options: UseArticleVersionOptions = {}): UseArticleVersionReturn {
  /**
   * Handle version selection
   * Opens version in a new tab instead of replacing content
   * 
   * @param version - Version to select
   */
  const handleVersionSelect = async (version: ArticleVersion | null): Promise<void> => {
    if (!version || !options.selectedArticleId) return;

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
