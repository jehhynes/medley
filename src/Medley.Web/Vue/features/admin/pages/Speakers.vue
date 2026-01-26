<template>
    <vertical-menu 
      :display-name="userDisplayName"
      :is-authenticated="userIsAuthenticated"
    />

    <!-- Left Sidebar (Speaker List) -->
    <div class="sidebar left-sidebar" :class="{ 'show': leftSidebarVisible }">
      <div class="sidebar-header">
        <h6 class="sidebar-title sidebar-breadcrumb-title">
          <a href="/Admin/Settings">Settings</a>
          <i class="bi bi-chevron-right"></i>
          <span>Speakers</span>
        </h6>
      </div>
      
      <!-- Filter Buttons -->
      <div class="sidebar-filters">
        <div class="btn-group w-100" role="group">
          <button 
            type="button" 
            class="btn btn-sm"
            :class="filterMode === 'internal' ? 'btn-primary' : 'btn-outline-secondary'"
            @click="setFilter('internal')">
            Internal
          </button>
          <button 
            type="button" 
            class="btn btn-sm"
            :class="filterMode === 'all' ? 'btn-primary' : 'btn-outline-secondary'"
            @click="setFilter('all')">
            All
          </button>
        </div>
 
        <div class="input-group input-group-sm mt-3">
          <span class="input-group-text">
            <i class="bi bi-search"></i>
          </span>
          <input 
            type="text" 
            class="form-control" 
            placeholder="Search by name or email..."
            v-model="searchQuery"
            @input="debouncedSearch">
        </div>
      </div>

      <div class="sidebar-content">
        <div v-if="loading" class="loading-spinner">
          <div class="spinner-border spinner-border-sm" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
        </div>
        <div v-else-if="error" class="alert alert-danger" v-cloak>
          {{ error }}
        </div>
        <ul v-else-if="speakers.length > 0" class="list-view" v-cloak>
          <li v-for="speaker in speakers" :key="speaker.id" class="list-item">
            <a href="#" 
               class="list-item-content"
               :class="{ active: selectedSpeaker?.id === speaker.id }"
               @click.prevent="selectSpeaker(speaker.id)">
              <i class="list-item-icon bi bi-person"></i>
              <div class="list-item-body">
                <div class="list-item-title d-flex justify-content-between align-items-center">
                  <div>
                    <span>{{ speaker.name }}</span>
                    <span v-if="speaker.trustLevel" class="ms-2">
                      <i class="bi bi-shield-check" :class="getTrustLevelClass(speaker.trustLevel)"></i>
                    </span>
                  </div>
                  <span v-if="speaker.isInternal === true" class="badge bg-success" style="font-size: 0.65rem;">Internal</span>
                </div>
                <div class="list-item-subtitle">
                  <span v-if="speaker.email" class="me-2">{{ speaker.email }}</span>
                  <span>
                    <i class="bi bi-file-text"></i> {{ speaker.sourceCount }}
                  </span>
                </div>
              </div>
            </a>
          </li>
        </ul>
        <div v-else class="empty-state-small" v-cloak>
          <p class="text-muted">No speakers found</p>
        </div>
      </div>
    </div>

    <!-- Main Content -->
    <div class="main-content">
      <div v-if="!selectedSpeaker" class="empty-state" v-cloak>
        <div class="empty-state-icon">
          <i class="bi bi-person"></i>
        </div>
        <div class="empty-state-title">No Speaker Selected</div>
        <div class="empty-state-text">Select a speaker from the sidebar to view and edit details</div>
      </div>

      <div v-else class="speaker-detail" v-cloak>
        <div class="main-content-header d-flex justify-content-between align-items-center mb-4">
          <div>
            <h4 class="mb-1">{{ selectedSpeaker.name }}</h4>
            <p v-if="selectedSpeaker.email" class="text-muted mb-0">{{ selectedSpeaker.email }}</p>
            <p v-else class="text-muted mb-0">No email address</p>
          </div>
        </div>

        <div class="card">
          <div class="card-header">
            <h5 class="card-title mb-0">Speaker Details</h5>
          </div>
          <div class="card-body">
              <div class="alert alert-info">
                  <i class="bi bi-file-text me-2"></i>
                  This speaker appears in <strong>{{ selectedSpeaker.sourceCount }}</strong> source{{ selectedSpeaker.sourceCount !== 1 ? 's' : '' }}
              </div>
           
              <form @submit.prevent="saveSpeaker" class="form-layout">
                  <!-- Scope Field -->
                  <div class="mb-3 form-group-overlap">
                      <label class="form-label">Scope</label>
                      <select class="form-select" v-model="editingIsInternal">
                          <option :value="null">Unknown</option>
                          <option :value="true">Internal</option>
                          <option :value="false">External</option>
                      </select>
                  </div>

                  <!-- TrustLevel Field -->
                  <div class="mb-3 form-group-overlap">
                      <label class="form-label">Trust Level</label>
                      <select class="form-select" v-model="editingTrustLevel">
                          <option :value="null">Not Set</option>
                          <option value="High">High</option>
                          <option value="Medium">Medium</option>
                          <option value="Low">Low</option>
                      </select>
                  </div>

                  <!-- Action Buttons -->
                  <div>
                      <button type="submit"
                              class="btn btn-primary"
                              :disabled="isSaving || !hasChanges">
                          <span v-if="isSaving" class="spinner-border spinner-border-sm me-1"></span>
                          <i v-else class="bi bi-save me-1"></i>
                          {{ isSaving ? 'Saving...' : 'Save Speaker' }}
                      </button>
                      <button type="button"
                              class="btn btn-outline-secondary ms-2"
                              :disabled="isSaving || !hasChanges"
                              @click="cancelEdit">
                          Cancel
                      </button>
                  </div>

                  <!-- Save indicator -->
                  <div v-if="lastSaved" class="mt-3 text-muted small">
                      <i class="bi bi-check-circle me-1"></i>
                      Last saved: {{ formatTime(lastSaved) }}
                  </div>
              </form>
          </div>
        </div>
      </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';

// Define props to handle attributes passed by router
defineProps<{
  id?: string;
}>();
import { speakersClient } from '@/utils/apiClients';
import { useSidebarState } from '@/composables/useSidebarState';
import { getTrustLevelClass } from '@/utils/helpers';
import type { SpeakerListDto, SpeakerDetailDto, TrustLevel } from '@/types/api-client';

// Setup composables
const { leftSidebarVisible } = useSidebarState();
const router = useRouter();

// Reactive state
const speakers = ref<SpeakerListDto[]>([]);
const selectedSpeaker = ref<SpeakerDetailDto | null>(null);
const loading = ref<boolean>(false);
const error = ref<string | null>(null);
const isSaving = ref<boolean>(false);
const lastSaved = ref<Date | null>(null);
const userDisplayName = ref<string>(window.MedleyUser?.displayName || 'User');
const userIsAuthenticated = ref<boolean>(window.MedleyUser?.isAuthenticated || false);

// Filter and search
const filterMode = ref<'internal' | 'all'>('internal');
const searchQuery = ref<string>('');

// Editing state
const editingIsInternal = ref<boolean | null>(null);
const editingTrustLevel = ref<string | null>(null);

// Computed
const hasChanges = computed(() => {
  if (!selectedSpeaker.value) return false;
  return editingIsInternal.value !== selectedSpeaker.value.isInternal ||
         editingTrustLevel.value !== selectedSpeaker.value.trustLevel;
});

// Methods
const loadSpeakers = async (): Promise<void> => {
  loading.value = true;
  error.value = null;
  try {
    const isInternal = filterMode.value === 'internal' ? true : undefined;
    speakers.value = await speakersClient.getAll(isInternal, searchQuery.value || undefined);
  } catch (err: any) {
    error.value = 'Failed to load speakers: ' + err.message;
    console.error('Error loading speakers:', err);
  } finally {
    loading.value = false;
  }
};

const setFilter = async (mode: 'internal' | 'all'): Promise<void> => {
  filterMode.value = mode;
  await loadSpeakers();
};

let searchTimeout: number | null = null;
const debouncedSearch = (): void => {
  if (searchTimeout) {
    clearTimeout(searchTimeout);
  }
  searchTimeout = window.setTimeout(() => {
    loadSpeakers();
  }, 300);
};

const selectSpeaker = async (id: string): Promise<void> => {
  try {
    const speaker = await speakersClient.getById(id);
    selectedSpeaker.value = speaker;
    editingIsInternal.value = speaker.isInternal;
    editingTrustLevel.value = speaker.trustLevel;
    lastSaved.value = null;
    
    // Update URL
    await router.push({ query: { id } });
  } catch (err: any) {
    console.error('Error loading speaker:', err);
    error.value = 'Failed to load speaker: ' + err.message;
  }
};

const saveSpeaker = async (): Promise<void> => {
  if (!selectedSpeaker.value || isSaving.value || !hasChanges.value) return;

  isSaving.value = true;
  try {
    const updated = await speakersClient.update(selectedSpeaker.value.id, {
      isInternal: editingIsInternal.value,
      trustLevel: editingTrustLevel.value
    });

    selectedSpeaker.value = updated;
    lastSaved.value = new Date();

    // Update the speaker in the list
    const speakerInList = speakers.value.find(s => s.id === updated.id);
    if (speakerInList) {
      speakerInList.isInternal = updated.isInternal;
      speakerInList.trustLevel = updated.trustLevel;
    }
  } catch (err: any) {
    console.error('Error saving speaker:', err);
    alert('Failed to save speaker: ' + err.message);
  } finally {
    isSaving.value = false;
  }
};

const cancelEdit = (): void => {
  if (selectedSpeaker.value) {
    editingIsInternal.value = selectedSpeaker.value.isInternal;
    editingTrustLevel.value = selectedSpeaker.value.trustLevel;
  }
};

const formatDateTime = (date: string | Date): string => {
  return new Date(date).toLocaleString();
};

const formatTime = (date: Date | null): string => {
  if (!date) return '';
  return new Date(date).toLocaleTimeString();
};

// Lifecycle hooks
onMounted(async () => {
  await loadSpeakers();

  const urlParams = new URLSearchParams(window.location.search);
  const idParam = urlParams.get('id');
  
  if (idParam) {
    await selectSpeaker(idParam);
  }

  window.addEventListener('popstate', async () => {
    const urlParams = new URLSearchParams(window.location.search);
    const idParam = urlParams.get('id');
    
    if (idParam && selectedSpeaker.value?.id !== idParam) {
      await selectSpeaker(idParam);
    }
  });
});
</script>

<style scoped>

.sidebar-filters {
  padding: 0.75rem 1rem;
  border-bottom: 1px solid var(--bs-border-color);
}

.empty-state-small {
  padding: 2rem 1rem;
  text-align: center;
}

.speaker-detail {
  padding: 2rem;
  max-width: 800px;
}

.main-content-header {
  margin-bottom: 1.5rem;
}

.main-content-header h4 {
  margin: 0;
  font-size: 1.5rem;
  font-weight: 600;
}
</style>
