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
                'bestpractices': 'bi-shield-check'
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
        }
    }
};

