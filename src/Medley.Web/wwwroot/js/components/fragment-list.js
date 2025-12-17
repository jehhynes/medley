// Fragment List Component - List view for fragments in sidebar
const FragmentList = {
    name: 'FragmentList',
    template: '#fragment-list-template',
    props: {
        fragments: {
            type: Array,
            default: () => []
        },
        selectedId: {
            type: String,
            default: null
        },
        articleTypes: {
            type: Array,
            default: () => []
        }
    },
    methods: {
        selectFragment(fragment) {
            this.$emit('select', fragment);
        },
        formatDate(dateString) {
            return window.MedleyUtils.formatDate(dateString);
        },
        getSourceIcon(type) {
            return window.MedleyUtils.getSourceTypeIcon(type);
        },
        getFragmentCategoryIcon(category) {
            return window.MedleyUtils.getFragmentCategoryIcon(category, this.articleTypes);
        },
        getIconClass(icon) {
            return window.MedleyUtils.getIconClass(icon);
        },
        getConfidenceIcon(confidence) {
            return window.MedleyUtils.getConfidenceIcon(confidence);
        },
        getConfidenceColor(confidence) {
            return window.MedleyUtils.getConfidenceColor(confidence);
        },
        getConfidenceLabel(confidence) {
            return window.MedleyUtils.getConfidenceLabel(confidence);
        }
    }
};

