// Article Tree Component - Recursive tree view for articles
const ArticleTree = {
    name: 'ArticleTree',
    template: `
  <ul class="tree-view">
    <li v-for="article in articles" :key="article.id" class="tree-item">
      <div class="tree-item-content" :class="{ active: selectedId === article.id }" @click="selectArticle(article)">
        <button 
          v-if="hasChildren(article)" 
          class="tree-item-toggle" 
          :class="{ expanded: isExpanded(article.id) }"
          @click.stop="toggleExpand(article.id)">
          <i class="bi bi-chevron-right"></i>
        </button>
        <span v-else class="tree-item-toggle"></span>
        <i :class="['tree-item-icon'].concat(getIconClass(article.articleTypeIcon).split(' '))"></i>
        <span class="tree-item-label">{{ article.title }}</span>
        <button 
          class="tree-item-actions"
          @click.stop="toggleActionsMenu(article.id)"
          title="Actions">
          <i class="bi bi-three-dots"></i>
        </button>
        <div v-if="openMenuId === article.id" class="tree-actions-dropdown" @click.stop>
          <button class="tree-actions-dropdown-item" @click="createChild(article.id)">New Article</button>
        </div>
      </div>
      <article-tree 
        v-if="hasChildren(article) && isExpanded(article.id)"
        :articles="article.children"
        :selected-id="selectedId"
        :expanded-ids="expandedIds"
        @select="selectArticle"
        @toggle-expand="toggleExpand"
        @create-child="createChild"
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
        }
    },
    data() {
        return {
            openMenuId: null
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
        getIconClass(icon) {
            return window.MedleyUtils.getIconClass(icon);
        },
        toggleActionsMenu(articleId) {
            if (this.openMenuId === articleId) {
                this.openMenuId = null;
            } else {
                this.openMenuId = articleId;
            }
        },
        createChild(parentArticleId) {
            this.openMenuId = null;
            this.$emit('create-child', parentArticleId);
        }
    },
    mounted() {
        // Close dropdown when clicking outside
        document.addEventListener('click', () => {
            this.openMenuId = null;
        });
    }
};

