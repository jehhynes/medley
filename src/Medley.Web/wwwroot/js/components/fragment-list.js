// Fragment List Component - List view for fragments in sidebar
const FragmentList = {
    name: 'FragmentList',
    template: `
  <ul class="list-view">
    <li v-for="fragment in fragments" :key="fragment.id" class="list-item">
      <a href="#" 
         class="list-item-content" 
         :class="{ active: selectedId === fragment.id }"
         @click.prevent="selectFragment(fragment)">
        <i class="list-item-icon" :class="getIconClass(getFragmentCategoryIcon(fragment.category))"></i>
        <div class="list-item-body">
          <div class="list-item-title">{{ fragment.title || 'Untitled Fragment' }}</div>
          <div class="list-item-subtitle">
            {{ formatDate(fragment.sourceDate) }} • {{ fragment.sourceType }}
            <span v-if="fragment.similarity !== undefined" class="badge bg-info ms-2" :title="'Similarity: ' + (fragment.similarity * 100).toFixed(1) + '%'">
              {{ (fragment.similarity * 100).toFixed(0) }}%
            </span>
            <span v-if="fragment.confidence !== null && fragment.confidence !== undefined" class="ms-2">
              <i 
                :class="'fa-duotone ' + getConfidenceIcon(fragment.confidence)" 
                :style="{ color: getConfidenceColor(fragment.confidence) }"
                :title="'Confidence: ' + getConfidenceLabel(fragment.confidence)"
              ></i>
            </span>
          </div>
        </div>
      </a>
    </li>
  </ul>
    `,
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
            // Collapse left sidebar on mobile after selection
            window.MedleySidebar?.collapseLeftSidebar();
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

