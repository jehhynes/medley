import { ref, computed, readonly, type Ref, type ComputedRef } from 'vue';
import { articlesClient } from '@/utils/apiClients';
import type { ArticleVersionDto, VersionType } from '@/types/api-client';

/** Public read-only store interface provided to child components */
export interface ArticleVersionsStore {
  readonly versions: Readonly<Ref<ArticleVersionDto[]>>;
  readonly loading: Readonly<Ref<boolean>>;
  readonly error: Readonly<Ref<string | null>>;
  readonly userVersions: ComputedRef<ArticleVersionDto[]>;
  readonly aiVersions: ComputedRef<ArticleVersionDto[]>;
  readonly pendingAiVersion: ComputedRef<ArticleVersionDto | undefined>;
  getVersionById(versionId: string): ArticleVersionDto | undefined;
}

/** Private internal API for owner (Articles.vue) only */
interface ArticleVersionsStoreInternal {
  loadVersions(articleId: string): Promise<void>;
  clearVersions(): void;
  handleVersionCreated(version: ArticleVersionDto): void;
  handleVersionUpdated(version: ArticleVersionDto): void;
  handleVersionDeleted(versionId: string): void;
}

/** Creates a page-scoped versions store with public and private APIs */
export function createArticleVersionsStore() {
  // Private state
  const versions = ref<ArticleVersionDto[]>([]);
  const loading = ref<boolean>(false);
  const error = ref<string | null>(null);
  let currentRequestId = 0;

  // Computed properties
  const userVersions = computed(() => 
    versions.value.filter(v => v.versionType === 'User' as VersionType)
  );

  const aiVersions = computed(() => 
    versions.value.filter(v => v.versionType === 'AI' as VersionType)
  );

  const pendingAiVersion = computed(() => 
    versions.value.find(v => v.status === 'PendingAiVersion')
  );

  // Query method (public)
  function getVersionById(versionId: string): ArticleVersionDto | undefined {
    return versions.value.find(v => v.id === versionId);
  }

  // Private mutation methods (NOT exposed in public API)
  async function loadVersions(articleId: string): Promise<void> {
    const requestId = ++currentRequestId;
    loading.value = true;
    error.value = null;

    try {
      const result = await articlesClient.getVersionHistory(articleId);
      if (requestId === currentRequestId) {
        versions.value = result;
      }
    } catch (err: any) {
      if (requestId === currentRequestId) {
        error.value = 'Failed to load version history: ' + err.message;
        console.error('Error loading versions:', err);
      }
    } finally {
      if (requestId === currentRequestId) {
        loading.value = false;
      }
    }
  }

  function clearVersions(): void {
    versions.value = [];
    error.value = null;
  }

  function handleVersionCreated(version: ArticleVersionDto): void {
    versions.value = [version, ...versions.value];
  }

  function handleVersionUpdated(version: ArticleVersionDto): void {
    const index = versions.value.findIndex(v => v.id === version.id);
    if (index !== -1) {
      versions.value[index] = version;
    }
  }

  function handleVersionDeleted(versionId: string): void {
    versions.value = versions.value.filter(v => v.id !== versionId);
  }

  // Return public API (provided to children) + private API (owner only)
  return {
    // Public read-only store
    store: {
      versions: readonly(versions),
      loading: readonly(loading),
      error: readonly(error),
      userVersions,
      aiVersions,
      pendingAiVersion,
      getVersionById
    } as ArticleVersionsStore,
    
    // Private API for owner only
    _internal: {
      loadVersions,
      clearVersions,
      handleVersionCreated,
      handleVersionUpdated,
      handleVersionDeleted
    } as ArticleVersionsStoreInternal
  };
}
