import { ref, nextTick, type Ref } from 'vue';
import { api } from '@/utils/api';
import type { ArticleDto } from '@/types/generated/api-client';

/**
 * Create modal state interface
 */
export interface CreateModalState {
  visible: boolean;
  title: string;
  typeId: string | null;
  parentId: string | null;
  isSubmitting: boolean;
}

/**
 * Edit modal state interface
 */
export interface EditModalState {
  visible: boolean;
  articleId: string | null;
  title: string;
  typeId: string | null;
  isSubmitting: boolean;
}

/**
 * Options for useArticleModal composable
 */
export interface UseArticleModalOptions {
  /**
   * Function to insert an article into the tree
   */
  insertArticleIntoTree: (article: ArticleDto) => void;

  /**
   * Function to update an article in the tree
   */
  updateArticleInTree: (articleId: string, updates: Partial<ArticleDto>) => void;

  /**
   * Function to select an article
   */
  selectArticle: (article: ArticleDto, shouldJoinSignalR?: boolean) => Promise<void>;

  /**
   * Articles index map
   */
  articlesIndex: Map<string, ArticleDto>;

  /**
   * Current selected article ID
   */
  selectedArticleId: Ref<string | null>;

  /**
   * Optional ref to the title input element for auto-focus
   */
  titleInputRef?: Ref<HTMLInputElement | null>;

  /**
   * Optional ref to the edit title input element for auto-focus
   */
  editTitleInputRef?: Ref<HTMLInputElement | null>;

  /**
   * Optional ref to the TipTap editor for syncing heading
   */
  tiptapEditorRef?: Ref<{ syncHeading: (title: string) => void } | null>;

  /**
   * Optional function to close sidebar menu
   */
  closeSidebarMenu?: () => void;
}

/**
 * Return type for useArticleModal composable
 */
interface UseArticleModalReturn {
  createModal: Ref<CreateModalState>;
  editModal: Ref<EditModalState>;
  validateArticleForm: (title: string | null | undefined, typeId: string | null | undefined) => boolean;
  showCreateArticleModal: (parentArticleId: string | null) => void;
  closeCreateModal: () => void;
  createArticle: () => Promise<void>;
  showEditArticleModal: (article: ArticleDto) => void;
  closeEditModal: () => void;
  updateArticle: () => Promise<void>;
}

/**
 * Composable for article create and edit modal management.
 * Handles modal state, validation, and API operations.
 * 
 * @param options - Configuration options for article modals
 * @returns Modal state and control methods
 */
export function useArticleModal(options: UseArticleModalOptions): UseArticleModalReturn {
  // Create modal state
  const createModal = ref<CreateModalState>({
    visible: false,
    title: '',
    typeId: null,
    parentId: null,
    isSubmitting: false
  });

  // Edit modal state
  const editModal = ref<EditModalState>({
    visible: false,
    articleId: null,
    title: '',
    typeId: null,
    isSubmitting: false
  });

  /**
   * Validate article form inputs
   * @param title - Article title
   * @param typeId - Article type ID
   * @returns True if validation passes
   */
  const validateArticleForm = (
    title: string | null | undefined,
    typeId: string | null | undefined
  ): boolean => {
    if (!title?.trim()) {
      (window as any).bootbox?.alert({
        title: 'Validation Error',
        message: 'Please enter a title',
        className: 'bootbox-warning'
      });
      return false;
    }
    if (!typeId) {
      (window as any).bootbox?.alert({
        title: 'Validation Error',
        message: 'Please select an article type',
        className: 'bootbox-warning'
      });
      return false;
    }
    return true;
  };

  /**
   * Show the create article modal
   * @param parentArticleId - ID of parent article (null for root)
   */
  const showCreateArticleModal = (parentArticleId: string | null): void => {
    options.closeSidebarMenu?.();
    createModal.value.parentId = parentArticleId;
    createModal.value.title = '';
    createModal.value.typeId = null;
    createModal.value.visible = true;

    nextTick(() => {
      if (options.titleInputRef?.value) {
        options.titleInputRef.value.focus();
      }
    });
  };

  /**
   * Close the create modal
   */
  const closeCreateModal = (): void => {
    createModal.value.visible = false;
    createModal.value.title = '';
    createModal.value.typeId = null;
    createModal.value.parentId = null;
  };

  /**
   * Create a new article
   */
  const createArticle = async (): Promise<void> => {
    if (!validateArticleForm(createModal.value.title, createModal.value.typeId)) {
      return;
    }

    createModal.value.isSubmitting = true;
    try {
      const response = await api.post<ArticleDto>('/api/articles', {
        title: createModal.value.title,
        articleTypeId: createModal.value.typeId,
        parentArticleId: createModal.value.parentId
      });

      closeCreateModal();

      // Insert the new article into the tree surgically
      options.insertArticleIntoTree(response);

      // Auto-select the newly created article
      if (response?.id) {
        const newArticle = options.articlesIndex.get(response.id);
        if (newArticle) {
          await options.selectArticle(newArticle, false);
        }
      }
    } catch (err: any) {
      (window as any).bootbox?.alert({
        title: 'Create Article Failed',
        message: `Failed to create article: ${err.message || 'Unknown error'}`,
        className: 'bootbox-error'
      });
      console.error('Error creating article:', err);
    } finally {
      createModal.value.isSubmitting = false;
    }
  };

  /**
   * Show the edit article modal
   * @param article - Article to edit
   */
  const showEditArticleModal = (article: ArticleDto): void => {
    editModal.value.articleId = article.id || null;
    editModal.value.title = article.title || '';
    editModal.value.typeId = article.articleTypeId || null;
    editModal.value.visible = true;

    nextTick(() => {
      if (options.editTitleInputRef?.value) {
        options.editTitleInputRef.value.focus();
      }
    });
  };

  /**
   * Close the edit modal
   */
  const closeEditModal = (): void => {
    editModal.value.visible = false;
    editModal.value.articleId = null;
    editModal.value.title = '';
    editModal.value.typeId = null;
  };

  /**
   * Update an existing article
   */
  const updateArticle = async (): Promise<void> => {
    if (!validateArticleForm(editModal.value.title, editModal.value.typeId)) {
      return;
    }

    if (!editModal.value.articleId) {
      console.error('No article ID for update');
      return;
    }

    editModal.value.isSubmitting = true;
    try {
      await api.put(`/api/articles/${editModal.value.articleId}/metadata`, {
        title: editModal.value.title,
        articleTypeId: editModal.value.typeId
      });

      // Update the article in the tree surgically
      options.updateArticleInTree(editModal.value.articleId, {
        title: editModal.value.title,
        articleTypeId: editModal.value.typeId
      });

      // If the article is currently selected, sync the first H1 in the editor
      if (options.selectedArticleId.value === editModal.value.articleId) {
        options.tiptapEditorRef?.value?.syncHeading(editModal.value.title);
      }

      closeEditModal();
    } catch (err: any) {
      (window as any).bootbox?.alert({
        title: 'Update Article Failed',
        message: `Failed to update article: ${err.message || 'Unknown error'}`,
        className: 'bootbox-error'
      });
      console.error('Error updating article:', err);
    } finally {
      editModal.value.isSubmitting = false;
    }
  };

  return {
    createModal,
    editModal,
    validateArticleForm,
    showCreateArticleModal,
    closeCreateModal,
    createArticle,
    showEditArticleModal,
    closeEditModal,
    updateArticle
  };
}
