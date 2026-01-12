<template>
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
                :title="'Confidence: ' + (fragment.confidence || '')"
              ></i>
            </span>
          </div>
        </div>
      </a>
    </li>
  </ul>
</template>

<script>
import { 
  formatDate, 
  getSourceTypeIcon, 
  getFragmentCategoryIcon, 
  getIconClass, 
  getConfidenceIcon, 
  getConfidenceColor
} from '@/utils/helpers.js';

export default {
  name: 'FragmentList',
  props: {
    fragments: {
      type: Array,
      default: () => []
    },
    selectedId: {
      type: String,
      default: null
    }
  },
  emits: ['select'],
  methods: {
    selectFragment(fragment) {
      this.$emit('select', fragment);
      // Collapse left sidebar on mobile after selection
      window.MedleySidebar?.collapseLeftSidebar();
    },
    // Expose imported utility functions to template
    formatDate,
    getSourceIcon: getSourceTypeIcon,
    getFragmentCategoryIcon,
    getIconClass,
    getConfidenceIcon,
    getConfidenceColor
  }
};
</script>
