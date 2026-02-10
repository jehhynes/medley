import { ref, nextTick, type Ref } from 'vue';
import { articlesClient } from '@/utils/apiClients';
import type { ArticleDto, ArticleSummaryDto, ArticleCreateRequest, ArticleUpdateMetadataRequest, ArticleStatus } from '@/types/api-client';

export interface CreateModalState {
  visible: boolean;
  title: string;
  typeId: string | null;
  parentId: string | null;
  isSubmitting: boolean;
}

export interface EditModalState {
  visible: boolean;
  articleId: string | null;
  title: string;
  typeId: string | null;
  status: ArticleStatus | null;
  isSubmitting: boolean;
}

export interface UseArticleModalOptions {
  insertArticleIntoTree: (article: ArticleSummaryDto) => void;
  updateArticleInTree: (articleId: string, updates: Partial<ArticleSummaryDto>) => void;
  selectArticle: (article: ArticleSummaryDto, shouldJoinSignalR?: boolean) => Promise<void>;
  articlesIndex: Map<string, ArticleDto>;
  selectedArticleId: Ref<string | null>;
  titleInputRef?: Ref<HTMLInputElement | null>;
  editTitleInputRef?: Ref<HTMLInputElement | null>;
  tiptapEditorRef?: Ref<{ syncHeading: (title: string) => void } | null>;
  closeSidebarMenu?: () => void;
}

/** Manages article create and edit modal state and operations */
export function useArticleModal(options: UseArticleModalOptions) {
  const createModal = ref<CreateModalState>({
    visible: false,
    title: '',
    typeId: null,
    parentId: null,
    isSubmitting: false
  });

  const editModal = ref<EditModalState>({
    visible: false,
    articleId: null,
    title: '',
    typeId: null,
    status: null,
    isSubmitting: false
  });

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

  const showCreateArticleModal = (parentArticleId: string | null) => {
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

  const closeCreateModal = () => {
    createModal.value.visible = false;
    createModal.value.title = '';
    createModal.value.typeId = null;
    createModal.value.parentId = null;
  };

  const createArticle = async () => {
    if (!validateArticleForm(createModal.value.title, createModal.value.typeId)) {
      return;
    }

    createModal.value.isSubmitting = true;
    try {
      const request: ArticleCreateRequest = {
        title: createModal.value.title!,
        articleTypeId: createModal.value.typeId!,
        parentArticleId: createModal.value.parentId || undefined
      };
      
      const response = await articlesClient.create(request);

      closeCreateModal();

      options.insertArticleIntoTree(response);

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

  const showEditArticleModal = (article: ArticleDto) => {
    editModal.value.articleId = article.id || null;
    editModal.value.title = article.title || '';
    editModal.value.typeId = article.articleTypeId || null;
    editModal.value.status = article.status ?? null;
    editModal.value.visible = true;

    nextTick(() => {
      if (options.editTitleInputRef?.value) {
        options.editTitleInputRef.value.focus();
      }
    });
  };

  const closeEditModal = () => {
    editModal.value.visible = false;
    editModal.value.articleId = null;
    editModal.value.title = '';
    editModal.value.typeId = null;
    editModal.value.status = null;
  };

  const updateArticle = async () => {
    if (!validateArticleForm(editModal.value.title, editModal.value.typeId)) {
      return;
    }

    if (!editModal.value.articleId) {
      console.error('No article ID for update');
      return;
    }

    editModal.value.isSubmitting = true;
    try {
      const request: ArticleUpdateMetadataRequest = {
        title: editModal.value.title!,
        articleTypeId: editModal.value.typeId!,
        status: editModal.value.status ?? undefined
      };
      
      await articlesClient.updateMetadata(editModal.value.articleId, request);

      options.updateArticleInTree(editModal.value.articleId, {
        title: editModal.value.title,
        articleTypeId: editModal.value.typeId,
        status: editModal.value.status ?? undefined
      });

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
