// Article Tree Component - Recursive tree view for articles
const ArticleTree = {
    name: 'ArticleTree',
    template: '#article-tree-template',
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
            if (!icon) {
                return 'bi bi-file-text';
            }
            // If it's a Bootstrap Icon (starts with bi-), add bi base class
            if (icon.startsWith('bi-')) {
                return `bi ${icon}`;
            }
            // If it's a Font Awesome icon (starts with fa-), add fas (solid) base class
            if (icon.startsWith('fa-')) {
                return `fas ${icon}`;
            }
            // Default fallback
            return 'bi bi-file-text';
        }
    }
};

