<template>
  <ul class="list-view">
    <li v-for="source in sources" :key="source.id" class="list-item">
      <a href="#" 
         class="list-item-content" 
         :class="{ active: selectedId === source.id }"
         @click.prevent="selectSource(source)">
        <i class="list-item-icon bi" :class="getSourceIcon(source.type)"></i>
        <div class="list-item-body">
          <div class="list-item-title">{{ source.name || 'Untitled' }}</div>
          <div class="list-item-subtitle">{{ formatDate(source.date) }} • {{ source.type }}</div>
        </div>
        <span class="list-item-status ms-auto">
          <span v-if="source.extractionStatus === 'InProgress'" 
                class="spinner-border spinner-border-sm text-secondary" 
                role="status"
                style="width: 1em; height: 1em;">
            <span class="visually-hidden">Extracting...</span>
          </span>
          <i v-else-if="source.extractionStatus === 'Completed'" 
             class="bi bi-check-lg text-success"></i>
          <i v-else-if="source.extractionStatus === 'Failed'" 
             class="bi bi-x-square text-danger"></i>
        </span>
      </a>
    </li>
  </ul>
</template>

<script>
export default {
  name: 'SourceList',
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
  emits: ['select'],
  methods: {
    selectSource(source) {
      this.$emit('select', source);
      // Collapse left sidebar on mobile after selection
      window.MedleySidebar?.collapseLeftSidebar();
    },
    formatDate(dateString) {
      return window.MedleyUtils.formatDate(dateString);
    },
    getSourceIcon(type) {
      return window.MedleyUtils.getSourceTypeIcon(type);
    }
  }
};
</script>
