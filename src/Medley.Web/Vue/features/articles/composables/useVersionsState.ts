import { ref, computed, type Ref, type ComputedRef } from 'vue';
import type { ArticleVersionDto, VersionType, VersionStatus } from '@/types/api-client';

interface VersionsState {
  versions: Ref<ArticleVersionDto[]>;
  loading: Ref<boolean>;
  error: Ref<string | null>;
  loaded: Ref<boolean>;
}

const versionsCache = new Map<string, VersionsState>();

/** Manages article versions with caching per article */
export function useVersionsState(articleId: Ref<string | null | undefined>) {
  const getOrCreateVersionsState = (id: string): VersionsState => {
    if (!versionsCache.has(id)) {
      versionsCache.set(id, {
        versions: ref<ArticleVersionDto[]>([]),
        loading: ref<boolean>(false),
        error: ref<string | null>(null),
        loaded: ref<boolean>(false)
      });
    }
    return versionsCache.get(id)!;
  };

  const state = computed(() => 
    articleId?.value ? getOrCreateVersionsState(articleId.value) : null
  );

  const versions = computed(() => state.value?.versions.value || []);
  const loading = computed(() => state.value?.loading.value ?? false);
  const error = computed(() => state.value?.error.value ?? null);
  const loaded = computed(() => state.value?.loaded.value ?? false);

  const userVersions = computed(() => 
    versions.value.filter(v => v.versionType === 'User' as VersionType)
  );

  const pendingAiVersion = computed(() => 
    versions.value.find(v => v.status === 'PendingAiVersion')
  );

  const getVersionById = (versionId: string) => {
    return versions.value.find(v => v.id === versionId);
  };

  const setVersions = (newVersions: ArticleVersionDto[]) => {
    if (state.value) {
      state.value.versions.value = newVersions;
      state.value.loaded.value = true;
    }
  };

  const setLoading = (isLoading: boolean) => {
    if (state.value) {
      state.value.loading.value = isLoading;
    }
  };

  const setError = (err: string | null) => {
    if (state.value) {
      state.value.error.value = err;
    }
  };

  const clearCache = (id: string) => {
    versionsCache.delete(id);
  };

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
  };
}
