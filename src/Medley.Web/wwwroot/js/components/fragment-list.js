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
        },
        getFragmentCategoryIcon(category) {
            if (!category) {
                return 'bi-file-text';
            }
            
            // Normalize: extract only alphabetic characters and convert to lowercase
            const normalize = (str) => {
                if (!str) return '';
                return str.replace(/[^a-zA-Z]/g, '').toLowerCase();
            };
            
            const normalizedCategory = normalize(category);
            
            // Hardcoded icon mappings
            const hardcodedIcons = {
                'bestpractice': 'bi-shield-check'
            };
            
            if (hardcodedIcons[normalizedCategory]) {
                return hardcodedIcons[normalizedCategory];
            }
            
            // Match fragment category to article type by normalized name
            if (this.articleTypes && this.articleTypes.length > 0) {
                const matchingType = this.articleTypes.find(
                    at => at.name && normalize(at.name) === normalizedCategory
                );
                
                if (matchingType && matchingType.icon) {
                    return matchingType.icon;
                }
            }
            
            // Default fallback
            return 'bi-file-text';
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
        },
        getConfidenceIcon(confidence) {
            if (!confidence) return 'fa-signal-bars-weak';
            const level = confidence.toString().toLowerCase();
            switch(level) {
                case 'high': return 'fa-signal-bars';
                case 'medium': return 'fa-signal-bars-good';
                case 'low': return 'fa-signal-bars-fair';
                case 'unclear': return 'fa-signal-bars-weak';
                default: return 'fa-signal-bars-weak';
            }
        },
        getConfidenceColor(confidence) {
            if (!confidence) return 'var(--bs-secondary)';
            const level = confidence.toString().toLowerCase();
            switch(level) {
                case 'high': return 'var(--bs-success)';
                case 'medium': return 'var(--bs-warning)';
                case 'low': return 'var(--bs-danger)';
                case 'unclear': return 'var(--bs-danger)';
                default: return 'var(--bs-secondary)';
            }
        },
        getConfidenceLabel(confidence) {
            if (!confidence) return '';
            return confidence.toString();
        }
    }
};

