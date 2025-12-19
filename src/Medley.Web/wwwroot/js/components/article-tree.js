// Article Tree Component - Recursive tree view for articles
// Shared drag state across all tree instances
const dragState = {
    draggingArticleId: null,
    dragOverId: null
};

const ArticleTree = {
    name: 'ArticleTree',
    template: `
  <ul class="tree-view">
    <li v-for="article in articles" :key="article.id" class="tree-item">
      <div 
        class="tree-item-content" 
        :class="{ 
          active: selectedId === article.id,
          'drag-over': dragState.dragOverId === article.id
        }" 
        draggable="true"
        @click="selectArticle(article)"
        @dragstart="handleDragStart($event, article)"
        @dragover="handleDragOver($event, article)"
        @dragleave="handleDragLeave($event, article)"
        @drop="handleDrop($event, article)">
        <button 
          v-if="hasChildren(article)" 
          class="tree-item-toggle" 
          :class="{ expanded: isExpanded(article.id) }"
          @click.stop="toggleExpand(article.id)">
          <i class="bi bi-chevron-right"></i>
        </button>
        <span v-else class="tree-item-toggle"></span>
        <i :class="['tree-item-icon'].concat(getIconClass(getArticleIcon(article)).split(' '))"></i>
        <span class="tree-item-label">{{ article.title }}</span>
        <div class="dropdown d-inline-block">
          <button 
            class="tree-item-actions"
            :id="'dropdown-' + article.id"
            data-bs-toggle="dropdown"
            data-bs-auto-close="true"
            aria-expanded="false"
            @click.stop
            title="Actions">
            <i class="bi bi-three-dots"></i>
          </button>
          <ul class="dropdown-menu dropdown-menu-end" :aria-labelledby="'dropdown-' + article.id">
            <li><button class="dropdown-item" @click.stop="editArticle(article)">Edit</button></li>
            <li><button class="dropdown-item" @click.stop="createChild(article.id)">New Article</button></li>
          </ul>
        </div>
      </div>
      <article-tree 
        v-if="hasChildren(article) && isExpanded(article.id)"
        :articles="article.children"
        :selected-id="selectedId"
        :expanded-ids="expandedIds"
        :article-type-icon-map="articleTypeIconMap"
        :article-types="articleTypes"
        @select="selectArticle"
        @toggle-expand="toggleExpand"
        @create-child="createChild"
        @edit-article="editArticle"
        @move-article="moveArticle"
        class="tree-children"
      />
    </li>
  </ul>
    `,
    props: {
        articles: {
            type: Array,
            default: () => []
        },
        selectedId: {
            type: String,
            default: null
        },
        expandedIds: {
            type: Set,
            default: () => new Set()
        },
        articleTypeIconMap: {
            type: Object,
            default: () => ({})
        },
        articleTypes: {
            type: Array,
            default: () => []
        }
    },
    data() {
        return {
            // Use shared drag state
            dragState: dragState,
            dragCounter: 0
        };
    },
    methods: {
        toggleExpand(articleId) {
            this.$emit('toggle-expand', articleId);
        },
        isExpanded(articleId) {
            return this.expandedIds.has(articleId);
        },
        selectArticle(article) {
            this.$emit('select', article);
        },
        hasChildren(article) {
            return article.children && article.children.length > 0;
        },
        getArticleIcon(article) {
            // Look up icon from dictionary, fallback to bi-file-text
            if (article.articleTypeId && this.articleTypeIconMap[article.articleTypeId]) {
                return this.articleTypeIconMap[article.articleTypeId];
            }
            return 'bi-file-text';
        },
        getIconClass(icon) {
            return window.MedleyUtils.getIconClass(icon);
        },
        createChild(parentArticleId) {
            this.$emit('create-child', parentArticleId);
        },
        editArticle(article) {
            this.$emit('edit-article', article);
        },
        isIndexType(article) {
            if (!article.articleTypeId) {
                return false;
            }
            const articleType = this.articleTypes.find(t => t.id === article.articleTypeId);
            return articleType && articleType.name.toLowerCase() === 'index';
        },
        handleDragStart(event, article) {
            this.dragState.draggingArticleId = article.id;
            event.dataTransfer.effectAllowed = 'move';
            event.dataTransfer.setData('text/plain', article.id);
            
            // Add a semi-transparent drag image
            const dragImage = event.target.cloneNode(true);
            dragImage.style.opacity = '0.5';
            event.dataTransfer.setDragImage(event.target, 0, 0);
        },
        handleDragOver(event, article) {
            // Don't allow dropping on itself
            if (article.id === this.dragState.draggingArticleId) {
                return;
            }
            
            // Only allow dropping on Index type articles
            if (!this.isIndexType(article)) {
                return;
            }
            
            event.preventDefault();
            event.stopPropagation();
            event.dataTransfer.dropEffect = 'move';
            
            // Set drag over state
            if (this.dragState.dragOverId !== article.id) {
                this.dragState.dragOverId = article.id;
                this.dragCounter = 0;
            }
        },
        handleDragLeave(event, article) {
            // Only clear drag over state if we're actually leaving the element
            // Use relatedTarget to check if we're entering a child element
            const rect = event.currentTarget.getBoundingClientRect();
            const x = event.clientX;
            const y = event.clientY;
            
            // Check if the mouse is still within the bounds of the element
            if (x < rect.left || x >= rect.right || y < rect.top || y >= rect.bottom) {
                if (this.dragState.dragOverId === article.id) {
                    this.dragState.dragOverId = null;
                    this.dragCounter = 0;
                }
            }
        },
        handleDrop(event, targetArticle) {
            event.preventDefault();
            event.stopPropagation();
            
            // Clear drag state
            this.dragState.dragOverId = null;
            this.dragCounter = 0;
            
            if (!this.dragState.draggingArticleId || this.dragState.draggingArticleId === targetArticle.id) {
                this.dragState.draggingArticleId = null;
                return;
            }
            
            // Only allow dropping on Index type articles
            if (!this.isIndexType(targetArticle)) {
                this.dragState.draggingArticleId = null;
                return;
            }
            
            // Emit the move event to parent
            this.$emit('move-article', this.dragState.draggingArticleId, targetArticle.id);
            
            this.dragState.draggingArticleId = null;
        },
        moveArticle(sourceId, targetId) {
            // Propagate the event up the tree
            this.$emit('move-article', sourceId, targetId);
        }
    }
};

