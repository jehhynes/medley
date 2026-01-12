import { api } from '@/utils/api.js';

// Article Modal Mixin - Handles create and edit article modals
export default {
    methods: {
        // === Validation ===
        validateArticleForm(title, typeId) {
            if (!title?.trim()) {
                bootbox.alert({
                    title: 'Validation Error',
                    message: 'Please enter a title',
                    className: 'bootbox-warning'
                });
                return false;
            }
            if (!typeId) {
                bootbox.alert({
                    title: 'Validation Error',
                    message: 'Please select an article type',
                    className: 'bootbox-warning'
                });
                return false;
            }
            return true;
        },

        // === Create Modal ===
        showCreateArticleModal(parentArticleId) {
            this.ui.sidebarMenuOpen = false;
            this.createModal.parentId = parentArticleId;
            this.createModal.title = '';
            this.createModal.typeId = null;
            this.createModal.visible = true;
            
            this.$nextTick(() => {
                if (this.$refs.titleInput) {
                    this.$refs.titleInput.focus();
                }
            });
        },

        closeCreateModal() {
            this.createModal.visible = false;
            this.createModal.title = '';
            this.createModal.typeId = null;
            this.createModal.parentId = null;
        },

        async createArticle() {
            if (!this.validateArticleForm(this.createModal.title, this.createModal.typeId)) {
                return;
            }

            this.createModal.isSubmitting = true;
            try {
                // Using imported api
                const response = await api.post('/api/articles', {
                    title: this.createModal.title,
                    articleTypeId: this.createModal.typeId,
                    parentArticleId: this.createModal.parentId
                });

                this.closeCreateModal();
                
                // Insert the new article into the tree surgically
                this.insertArticleIntoTree(response);

                // Auto-select the newly created article
                if (response && response.id) {
                    const newArticle = this.articles.index.get(response.id);
                    if (newArticle) {
                        await this.selectArticle(newArticle, false);
                    }
                }
            } catch (err) {
                bootbox.alert({
                    title: 'Create Article Failed',
                    message: `Failed to create article: ${err.message}`,
                    className: 'bootbox-error'
                });
                console.error('Error creating article:', err);
            } finally {
                this.createModal.isSubmitting = false;
            }
        },

        // === Edit Modal ===
        showEditArticleModal(article) {
            this.editModal.articleId = article.id;
            this.editModal.title = article.title;
            this.editModal.typeId = article.articleTypeId || null;
            this.editModal.visible = true;
            
            this.$nextTick(() => {
                if (this.$refs.editTitleInput) {
                    this.$refs.editTitleInput.focus();
                }
            });
        },

        closeEditModal() {
            this.editModal.visible = false;
            this.editModal.articleId = null;
            this.editModal.title = '';
            this.editModal.typeId = null;
        },

        async updateArticle() {
            if (!this.validateArticleForm(this.editModal.title, this.editModal.typeId)) {
                return;
            }

            this.editModal.isSubmitting = true;
            try {
                // Using imported api
                await api.put(`/api/articles/${this.editModal.articleId}/metadata`, {
                    title: this.editModal.title,
                    articleTypeId: this.editModal.typeId
                });

                // Update the article in the tree surgically
                this.updateArticleInTree(this.editModal.articleId, {
                    title: this.editModal.title,
                    articleTypeId: this.editModal.typeId
                });

                // If the article is currently selected, sync the first H1 in the editor
                if (this.articles.selectedId === this.editModal.articleId) {
                    this.$refs.tiptapEditor?.syncHeading(this.editModal.title);
                }

                this.closeEditModal();
            } catch (err) {
                bootbox.alert({
                    title: 'Update Article Failed',
                    message: `Failed to update article: ${err.message}`,
                    className: 'bootbox-error'
                });
                console.error('Error updating article:', err);
            } finally {
                this.editModal.isSubmitting = false;
            }
        }
    }
};
