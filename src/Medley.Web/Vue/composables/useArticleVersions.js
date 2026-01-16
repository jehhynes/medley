import { ref, computed } from 'vue'

const versionsCache = new Map()

export function useArticleVersions(articleId) {
  const getOrCreateVersionsState = (id) => {
    if (!versionsCache.has(id)) {
      versionsCache.set(id, {
        versions: ref([]),
        loading: ref(false),
        error: ref(null),
        loaded: ref(false)
      })
    }
    return versionsCache.get(id)
  }

  const state = computed(() => articleId?.value 
    ? getOrCreateVersionsState(articleId.value) 
    : null)

  const versions = computed(() => state.value?.versions.value || [])
  const loading = computed(() => state.value?.loading.value ?? false)
  const error = computed(() => state.value?.error.value ?? null)
  const loaded = computed(() => state.value?.loaded.value ?? false)

  const userVersions = computed(() => 
    versions.value.filter(v => v.versionType === 'User')
  )

  const pendingAiVersion = computed(() => 
    versions.value.find(v => v.status === 'PendingAiVersion')
  )

  const getVersionById = (versionId) => {
    return versions.value.find(v => v.id === versionId)
  }

  const setVersions = (newVersions) => {
    if (state.value) {
      state.value.versions.value = newVersions
      state.value.loaded.value = true
    }
  }

  const setLoading = (isLoading) => {
    if (state.value) state.value.loading.value = isLoading
  }

  const setError = (err) => {
    if (state.value) state.value.error.value = err
  }

  const clearCache = (id) => {
    versionsCache.delete(id)
  }

  return {
    versions,
    userVersions,
    pendingAiVersion,
    getVersionById,
    loading,
    error,
    loaded,
    setVersions,
    setLoading,
    setError,
    clearCache
  }
}
