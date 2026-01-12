<template>
  <div v-if="visible && fragment">
    <div class="modal fade show" id="fragmentModal" tabindex="-1" aria-labelledby="fragmentModalLabel" style="display: block;" @click.self="close">
      <div class="modal-dialog modal-xl">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title" id="fragmentModalLabel">
              {{ fragment.title || 'Untitled Fragment' }}
            </h5>
            <button type="button" class="btn-close" @click="close" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            <div class="mb-3">
              <span v-if="fragment.category" class="badge bg-secondary me-2">
                <i :class="getIconClass(getFragmentCategoryIcon(fragment.category))" class="me-1"></i>{{ fragment.category }}
              </span>
              <span 
                v-if="fragment.confidence !== null && fragment.confidence !== undefined && fragment.confidenceComment" 
                class="badge bg-light text-dark"
                @click="toggleConfidenceComment"
                style="cursor: pointer;"
                :title="showConfidenceComment ? 'Hide confidence note' : 'Show confidence note'">
                <i 
                  :class="'fa-duotone ' + getConfidenceIcon(fragment.confidence)" 
                  :style="{ color: getConfidenceColor(fragment.confidence) }"
                  class="me-1"
                ></i>
                Confidence: {{ getConfidenceLabel(fragment.confidence) }}
                <i :class="showConfidenceComment ? 'bi bi-chevron-up ms-1' : 'bi bi-chevron-down ms-1'"></i>
              </span>
              <span 
                v-else-if="fragment.confidence !== null && fragment.confidence !== undefined" 
                class="badge bg-light text-dark">
                <i 
                  :class="'fa-duotone ' + getConfidenceIcon(fragment.confidence)" 
                  :style="{ color: getConfidenceColor(fragment.confidence) }"
                  class="me-1"
                ></i>
                Confidence: {{ getConfidenceLabel(fragment.confidence) }}
              </span>
            </div>
            <div v-if="fragment.confidenceComment && showConfidenceComment" class="alert alert-info mb-3">
              <div class="d-flex align-items-start">
                <i class="bi bi-info-circle me-2 mt-1"></i>
                <div>
                  <strong>Confidence Note:</strong>
                  <div class="mt-1">{{ fragment.confidenceComment }}</div>
                </div>
              </div>
            </div>
            <div class="markdown-container" v-html="renderedMarkdown"></div>
          </div>
        </div>
      </div>
    </div>
    <div class="modal-backdrop fade show"></div>
  </div>
</template>

<script>
export default {
  name: 'FragmentModal',
  props: {
    fragment: {
      type: Object,
      default: null
    },
    visible: {
      type: Boolean,
      default: false
    }
  },
  emits: ['close'],
  data() {
    return {
      showConfidenceComment: false
    };
  },
  computed: {
    renderedMarkdown() {
      if (!this.fragment || !this.fragment.content) {
        return '';
      }
      if (typeof marked !== 'undefined') {
        return marked.parse(this.fragment.content);
      }
      return this.fragment.content.replace(/\n/g, '<br>');
    }
  },
  watch: {
    visible(newVal) {
      if (!newVal) {
        this.showConfidenceComment = false;
      }
    }
  },
  methods: {
    close() {
      this.$emit('close');
    },
    
    toggleConfidenceComment() {
      this.showConfidenceComment = !this.showConfidenceComment;
    },

    getIconClass(icon) {
      return window.MedleyUtils.getIconClass(icon);
    },

    getFragmentCategoryIcon(category) {
      return window.MedleyUtils.getFragmentCategoryIcon(category);
    },

    getConfidenceIcon(confidence) {
      return window.MedleyUtils.getConfidenceIcon(confidence);
    },

    getConfidenceColor(confidence) {
      return window.MedleyUtils.getConfidenceColor(confidence);
    },

    getConfidenceLabel(confidence) {
      return window.MedleyUtils.getConfidenceLabel(confidence);
    },

    handleKeydown(event) {
      if (event.key === 'Escape' && this.visible) {
        this.close();
      }
    }
  },

  mounted() {
    window.addEventListener('keydown', this.handleKeydown);
  },

  beforeUnmount() {
    window.removeEventListener('keydown', this.handleKeydown);
  }
};
</script>
