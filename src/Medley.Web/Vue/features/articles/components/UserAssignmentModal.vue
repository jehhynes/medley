<template>
  <Teleport to="body">
    <div 
      class="modal fade" 
      :class="{ show: isVisible }" 
      :style="{ display: isVisible ? 'block' : 'none' }"
      tabindex="-1"
      @click.self="close"
    >
      <div class="modal-dialog modal-dialog-scrollable">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Assign Article</h5>
            <button type="button" class="btn-close" @click="close"></button>
          </div>
          
          <div class="modal-body">
            <div v-if="loading" class="text-center py-4">
              <div class="spinner-border" role="status">
                <span class="visually-hidden">Loading...</span>
              </div>
            </div>

            <div v-else-if="error" class="alert alert-danger">
              {{ error }}
            </div>

            <div v-else class="user-list">
              <!-- Unassign option -->
              <div 
                class="list-group-item user-item unassign-item"
                @click="handleUnassign"
                :class="{ disabled: assigning }"
              >
                <div class="user-avatar unassign-avatar">
                  <i class="bi bi-person-x"></i>
                </div>
                <span class="user-name">Unassign</span>
              </div>

              <!-- User list -->
              <div 
                v-for="user in users" 
                :key="user.id"
                class="list-group-item user-item"
                @click="handleAssign(user)"
                :class="{ 
                  disabled: assigning,
                  active: currentUserId === user.id 
                }"
              >
                <div 
                  class="user-avatar" 
                  :style="{ backgroundColor: user.color || '#6c757d' }"
                  :title="user.fullName"
                >
                  {{ user.initials || '?' }}
                </div>
                <span class="user-name">{{ user.fullName }}</span>
                <i v-if="currentUserId === user.id" class="bi bi-check-circle-fill ms-auto text-success"></i>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
    
    <div 
      v-if="isVisible" 
      class="modal-backdrop fade show"
      @click="close"
    ></div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, watch, onMounted, onBeforeUnmount } from 'vue';
import { articlesClient, usersClient } from '@/utils/apiClients';
import type { UserSummaryDto } from '@/types/api-client';

const props = defineProps<{
  visible: boolean;
  articleId: string;
  currentUserId?: string | null;
}>();

const emit = defineEmits<{
  close: [];
  assigned: [user: UserSummaryDto | null];
}>();

const isVisible = ref(props.visible);
const loading = ref(false);
const assigning = ref(false);
const error = ref<string | null>(null);
const users = ref<UserSummaryDto[]>([]);

watch(() => props.visible, (newVal) => {
  isVisible.value = newVal;
  if (newVal) {
    loadUsers();
  }
});

// Handle ESC key to close modal
const handleEscape = (event: KeyboardEvent) => {
  if (event.key === 'Escape' && isVisible.value && !assigning.value) {
    close();
  }
};

onMounted(() => {
  document.addEventListener('keydown', handleEscape);
});

onBeforeUnmount(() => {
  document.removeEventListener('keydown', handleEscape);
});

const loadUsers = async () => {
  loading.value = true;
  error.value = null;
  try {
    users.value = await usersClient.getAll();
  } catch (err) {
    console.error('Failed to load users:', err);
    error.value = 'Failed to load users. Please try again.';
  } finally {
    loading.value = false;
  }
};

const handleAssign = async (user: UserSummaryDto) => {
  if (assigning.value) return;
  
  assigning.value = true;
  try {
    await articlesClient.assign(props.articleId, { userId: user.id! });
    emit('assigned', user);
    assigning.value = false;
    close();
  } catch (err) {
    console.error('Failed to assign user:', err);
    alert('Failed to assign user. Please try again.');
    assigning.value = false;
  }
};

const handleUnassign = async () => {
  if (assigning.value) return;
  
  assigning.value = true;
  try {
    await articlesClient.unassign(props.articleId);
    emit('assigned', null);
    assigning.value = false;
    close();
  } catch (err) {
    console.error('Failed to unassign user:', err);
    alert('Failed to unassign user. Please try again.');
    assigning.value = false;
  }
};

const close = () => {
  if (!assigning.value) {
    isVisible.value = false;
    emit('close');
  }
};
</script>

<style scoped>
.modal-dialog {
  max-width: 400px;
}

.user-list {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.user-item {
  display: flex;
  align-items: center;
  padding: 0.75rem 1rem;
  cursor: pointer;
  border: 1px solid var(--bs-border-color);
  border-top: none;
  transition: background-color 0.15s ease-in-out;
}

.user-item:first-child {
  border-top: 1px solid var(--bs-border-color);
  border-top-left-radius: 0.375rem;
  border-top-right-radius: 0.375rem;
}

.user-item:last-child {
  border-bottom-left-radius: 0.375rem;
  border-bottom-right-radius: 0.375rem;
}

.user-item:hover:not(.disabled) {
  background-color: var(--bs-secondary-bg);
}

.user-item.active {
  background-color: var(--bs-primary-bg-subtle);
}

.user-item.disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.user-avatar {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  border-radius: 50%;
  font-size: 0.85rem;
  font-weight: 600;
  color: white;
  flex-shrink: 0;
  margin-right: 0.75rem;
}

.unassign-item {
  color: var(--bs-secondary);
}

.unassign-avatar {
  background-color: var(--bs-secondary-bg) !important;
  color: var(--bs-secondary) !important;
  border: 1px solid var(--bs-border-color);
  font-size: 1rem;
}

.user-name {
  flex: 1;
  font-size: 0.95rem;
}
</style>
