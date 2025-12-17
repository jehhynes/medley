// Source List Component - List view for sources
const SourceList = {
    name: 'SourceList',
    template: '#source-list-template',
    props: {
        sources: {
            type: Array,
            default: () => []
        },
        selectedId: {
            type: String,
            default: null
        }
    },
    methods: {
        selectSource(source) {
            this.$emit('select', source);
        },
        formatDate(dateString) {
            return window.MedleyUtils.formatDate(dateString);
        },
        getSourceIcon(type) {
            return window.MedleyUtils.getSourceTypeIcon(type);
        }
    }
};

