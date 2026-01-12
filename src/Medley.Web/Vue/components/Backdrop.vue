<template>
  <div 
    class="mobile-sidebar-backdrop"
    :class="backdropClass"
    @click="handleClick"
    aria-hidden="true"
  ></div>
</template>

<script>
export default {
  name: 'Backdrop',
  props: {
    isAutomatic: {
      type: Boolean,
      default: false
    }
  },
  emits: ['click'],
  computed: {
    backdropClass() {
      // Use 'show-initial' for automatic opening (mobile-only backdrop)
      // Use 'show' for manual toggle (backdrop on all sizes up to xl)
      return this.isAutomatic ? 'show-initial' : 'show'
    }
  },
  methods: {
    handleClick() {
      this.$emit('click')
    }
  }
}
</script>