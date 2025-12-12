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
            if (!dateString) return 'N/A';
            const date = new Date(dateString);
            return date.toLocaleDateString();
        },
        getSourceIcon(type) {
            const icons = {
                'Meeting': 'bi-camera-video',
                'Document': 'bi-file-text',
                'Email': 'bi-envelope',
                'Chat': 'bi-chat-dots',
                'Repository': 'bi-git',
                'Other': 'bi-file-earmark'
            };
            return icons[type] || 'bi-file-earmark';
        }
    }
};

