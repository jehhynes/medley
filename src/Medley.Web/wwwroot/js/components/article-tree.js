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
        @select="selectArticle"
        @toggle-expand="toggleExpand"
        @create-child="createChild"
        @edit-article="editArticle"
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
        }
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
        }
    }
};

