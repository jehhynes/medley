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
      </div>
      <article-tree 
        v-if="hasChildren(article) && isExpanded(article.id)"
        :articles="article.children"
        :selected-id="selectedId"
        @select="selectArticle"
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
        }
    },
    data() {
        return {
            expandedIds: new Set()
        };
    },
    methods: {
        toggleExpand(articleId) {
            if (this.expandedIds.has(articleId)) {
                this.expandedIds.delete(articleId);
            } else {
                this.expandedIds.add(articleId);
            }
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
        }
    }
};

