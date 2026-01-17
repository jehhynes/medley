<template>
    <div class="tiptap-editor">
        <div class="tiptap-toolbar" v-if="editor">
            <div class="tiptap-toolbar-left">
                <!-- Custom buttons slot -->
                <slot name="toolbar-prepend"></slot>
                
                <!-- Formatting buttons (only show if showFormatting is true) -->
                <div v-if="showFormatting" class="tiptap-formatting-buttons">
                    <div class="tiptap-dropdown">
                        <button 
                            type="button"
                            @click="toggleHeadingDropdown"
                            :class="{ 'is-active': isHeadingActive }"
                            class="tiptap-toolbar-btn tiptap-dropdown-toggle"
                            title="Headings">
                            <span>H</span>
                            <i class="bi bi-chevron-down tiptap-dropdown-caret"></i>
                        </button>
                        <div v-show="showHeadingDropdown" class="tiptap-dropdown-menu">
                            <button 
                                type="button"
                                @click="setHeading(1)" 
                                :class="{ 'is-active': isHeading1Active }"
                                class="tiptap-dropdown-item">
                                <i class="bi bi-type-h1"></i> Heading 1
                            </button>
                            <button 
                                type="button"
                                @click="setHeading(2)" 
                                :class="{ 'is-active': isHeading2Active }"
                                class="tiptap-dropdown-item">
                                <i class="bi bi-type-h2"></i> Heading 2
                            </button>
                            <button 
                                type="button"
                                @click="setHeading(3)" 
                                :class="{ 'is-active': isHeading3Active }"
                                class="tiptap-dropdown-item">
                                <i class="bi bi-type-h3"></i> Heading 3
                            </button>
                            <button 
                                type="button"
                                @click="setHeading(4)" 
                                :class="{ 'is-active': isHeading4Active }"
                                class="tiptap-dropdown-item">
                                <span class="tiptap-h4-icon">H4</span> Heading 4
                            </button>
                            <div class="tiptap-dropdown-divider"></div>
                            <button 
                                type="button"
                                @click="setParagraph" 
                                :class="{ 'is-active': isParagraphActive && !isHeadingActive }"
                                class="tiptap-dropdown-item">
                                <i class="bi bi-paragraph"></i> Paragraph
                            </button>
                        </div>
                    </div>
                    <div class="tiptap-dropdown">
                        <button 
                            type="button"
                            @click="toggleListDropdown"
                            :class="{ 'is-active': isListActive }"
                            class="tiptap-toolbar-btn tiptap-dropdown-toggle"
                            title="Lists">
                            <i class="bi bi-list-ul"></i>
                            <i class="bi bi-chevron-down tiptap-dropdown-caret"></i>
                        </button>
                        <div v-show="showListDropdown" class="tiptap-dropdown-menu">
                            <button 
                                type="button"
                                @click="setList('bullet')" 
                                :class="{ 'is-active': isBulletListActive }"
                                class="tiptap-dropdown-item">
                                <i class="bi bi-list-ul"></i> Bullet List
                            </button>
                            <button 
                                type="button"
                                @click="setList('ordered')" 
                                :class="{ 'is-active': isOrderedListActive }"
                                class="tiptap-dropdown-item">
                                <i class="bi bi-list-ol"></i> Numbered List
                            </button>
                        </div>
                    </div>
                    <div class="tiptap-toolbar-divider"></div>
                    <button 
                        type="button"
                        @click="toggleBlockquote" 
                        :class="{ 'is-active': isBlockquoteActive }"
                        class="tiptap-toolbar-btn"
                        title="Quote">
                        <i class="bi bi-quote"></i>
                    </button>
                    <button 
                        type="button"
                        @click="toggleCodeBlock" 
                        :class="{ 'is-active': isCodeBlockActive }"
                        class="tiptap-toolbar-btn"
                        title="Code Block">
                        <i class="bi bi-code-square"></i>
                    </button>
                    <button 
                        type="button"
                        @click="insertHorizontalRule" 
                        class="tiptap-toolbar-btn"
                        title="Separator">
                        <i class="bi bi-dash-lg"></i>
                    </button>
                    <div class="tiptap-toolbar-divider"></div>
                    <button 
                        type="button"
                        @click="toggleBold" 
                        :class="{ 'is-active': isBoldActive }"
                        class="tiptap-toolbar-btn"
                        title="Bold">
                        <i class="bi bi-type-bold"></i>
                    </button>
                    <button 
                        type="button"
                        @click="toggleItalic" 
                        :class="{ 'is-active': isItalicActive }"
                        class="tiptap-toolbar-btn"
                        title="Italic">
                        <i class="bi bi-type-italic"></i>
                    </button>
                    <button 
                        type="button"
                        @click="toggleCode" 
                        :class="{ 'is-active': isCodeActive }"
                        class="tiptap-toolbar-btn"
                        title="Inline Code">
                        <i class="bi bi-code"></i>
                    </button>
                    <button 
                        type="button"
                        @click="toggleLink" 
                        :class="{ 'is-active': isLinkActive }"
                        class="tiptap-toolbar-btn"
                        title="Link">
                        <i class="bi bi-link-45deg"></i>
                    </button>
                    <div class="tiptap-toolbar-divider"></div>
                    <button 
                        type="button"
                        @click="insertTable" 
                        class="tiptap-toolbar-btn"
                        title="Insert Table">
                        <i class="bi bi-table"></i>
                    </button>
                    <button 
                        type="button"
                        @click="addColumnBefore" 
                        :disabled="!isInTable"
                        class="tiptap-toolbar-btn"
                        title="Add Column Before">
                        <i class="bi bi-layout-sidebar-inset"></i>
                    </button>
                    <button 
                        type="button"
                        @click="addColumnAfter" 
                        :disabled="!isInTable"
                        class="tiptap-toolbar-btn"
                        title="Add Column After">
                        <i class="bi bi-layout-sidebar-inset-reverse"></i>
                    </button>
                    <button 
                        type="button"
                        @click="addRowBefore" 
                        :disabled="!isInTable"
                        class="tiptap-toolbar-btn"
                        title="Add Row Before">
                        <i class="bi bi-layout-sidebar-inset" style="transform: rotate(90deg);"></i>
                    </button>
                    <button 
                        type="button"
                        @click="addRowAfter" 
                        :disabled="!isInTable"
                        class="tiptap-toolbar-btn"
                        title="Add Row After">
                        <i class="bi bi-layout-sidebar-inset-reverse" style="transform: rotate(90deg);"></i>
                    </button>
                    <div class="tiptap-dropdown">
                        <button 
                            type="button"
                            @click="toggleDeleteDropdown"
                            :disabled="!isInTable"
                            class="tiptap-toolbar-btn tiptap-dropdown-toggle"
                            title="Delete">
                            <i class="bi bi-trash"></i>
                            <i class="bi bi-chevron-down tiptap-dropdown-caret"></i>
                        </button>
                        <div v-show="showDeleteDropdown" class="tiptap-dropdown-menu">
                            <button 
                                type="button"
                                @click="deleteRow" 
                                class="tiptap-dropdown-item">
                                Remove Row
                            </button>
                            <button 
                                type="button"
                                @click="deleteColumn" 
                                class="tiptap-dropdown-item">
                                Remove Column
                            </button>
                            <div class="tiptap-dropdown-divider"></div>
                            <button 
                                type="button"
                                @click="deleteTable" 
                                class="tiptap-dropdown-item">
                                Remove Table
                            </button>
                        </div>
                    </div>
                    <div class="tiptap-toolbar-divider"></div>
                </div>
                
                <!-- Undo/Redo always visible -->
                <button 
                    type="button"
                    @click="undo" 
                    :disabled="!canUndo"
                    class="tiptap-toolbar-btn"
                    title="Undo">
                    <i class="bi bi-arrow-counterclockwise"></i>
                </button>
                <button 
                    type="button"
                    @click="redo" 
                    :disabled="!canRedo"
                    class="tiptap-toolbar-btn"
                    title="Redo">
                    <i class="bi bi-arrow-clockwise"></i>
                </button>
                
                <!-- Custom buttons slot after undo/redo -->
                <slot name="toolbar-append"></slot>
            </div>
            <div class="tiptap-toolbar-right">
                <!-- Show auto-save indicator when auto-save is enabled -->
                <div v-if="autoSave" class="auto-save-indicator">
                    <i :class="autoSaveIconClass" :title="autoSaveIndicatorText"></i>
                </div>
                <!-- Show Save button when showSaveButton is true -->
                <button 
                    v-if="showSaveButton"
                    type="button"
                    @click="handleSave" 
                    :disabled="isSaving || !hasChanges"
                    class="btn btn-primary btn-sm"
                    title="Save">
                    <span v-if="isSaving" class="spinner-border spinner-border-sm me-1"></span>
                    <i v-else class="bi bi-save"></i>
                    {{ isSaving ? 'Saving...' : 'Save' }}
                </button>
            </div>
        </div>
        <!-- Notifications slot for alerts and messages -->
        <slot name="notifications"></slot>
        <div ref="editorElement" class="tiptap-content"></div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue';
import { Editor } from '@tiptap/vue-3';
import StarterKit from '@tiptap/starter-kit';
import { Table } from '@tiptap/extension-table';
import TableRow from '@tiptap/extension-table-row';
import TableCell from '@tiptap/extension-table-cell';
import TableHeader from '@tiptap/extension-table-header';
import { Markdown } from 'tiptap-markdown';
import Link from '@tiptap/extension-link';

type AutoSaveState = 'saved' | 'changed' | 'saving';
type ListType = 'bullet' | 'ordered';

interface Props {
    modelValue?: string;
    isSaving?: boolean;
    autoSave?: boolean;
    showSaveButton?: boolean;
    showFormatting?: boolean;
    readonly?: boolean;
}

interface Emits {
    (e: 'update:modelValue', value: string): void;
    (e: 'save'): void;
}

const props = withDefaults(defineProps<Props>(), {
    modelValue: '',
    isSaving: false,
    autoSave: false,
    showSaveButton: true,
    showFormatting: true,
    readonly: false
});

const emit = defineEmits<Emits>();

// Refs
const editorElement = ref<HTMLElement | null>(null);
const editor = ref<Editor | null>(null);
const canUndo = ref(false);
const canRedo = ref(false);
const isInTable = ref(false);
const lastEmittedValue = ref('');
const originalContent = ref('');
const showHeadingDropdown = ref(false);
const showListDropdown = ref(false);
const showDeleteDropdown = ref(false);
const autoSaveTimer = ref<number | null>(null);
const autoSaveState = ref<AutoSaveState>('saved');
const isBoldActive = ref(false);
const isItalicActive = ref(false);
const isCodeActive = ref(false);
const isLinkActive = ref(false);
const isBlockquoteActive = ref(false);
const isCodeBlockActive = ref(false);
const isBulletListActive = ref(false);
const isOrderedListActive = ref(false);
const isParagraphActive = ref(false);
const isHeading1Active = ref(false);
const isHeading2Active = ref(false);
const isHeading3Active = ref(false);
const isHeading4Active = ref(false);

// Computed
const isHeadingActive = computed(() => 
    isHeading1Active.value || isHeading2Active.value || 
    isHeading3Active.value || isHeading4Active.value
);

const isListActive = computed(() => 
    isBulletListActive.value || isOrderedListActive.value
);

const hasChanges = computed(() => 
    lastEmittedValue.value !== originalContent.value
);

const autoSaveIndicatorText = computed(() => {
    if (autoSaveState.value === 'saving' || autoSaveState.value === 'changed') {
        return 'Saving...';
    }
    return 'Saved';
});

const autoSaveIconClass = computed(() => {
    if (autoSaveState.value === 'saving' || autoSaveState.value === 'changed') {
        return 'far fa-arrows-rotate fa-spin';
    }
    return 'far fa-cloud-check';
});

// Watchers
watch(() => props.modelValue, (newValue) => {
    if (!editor.value || editor.value.isDestroyed) return;
    if (lastEmittedValue.value === (newValue || '')) return;

    editor.value.commands.setContent(newValue || '', false);
    originalContent.value = newValue || '';
    lastEmittedValue.value = newValue || '';
});

watch(() => props.isSaving, (newValue, oldValue) => {
    if (oldValue === true && newValue === false) {
        originalContent.value = lastEmittedValue.value;
        if (props.autoSave) {
            autoSaveState.value = 'saved';
        }
    }
});

watch(() => props.readonly, (newValue) => {
    if (editor.value && !editor.value.isDestroyed) {
        editor.value.setEditable(!newValue);
    }
});

// Methods
const toggleBold = () => editor.value?.chain().focus().toggleBold().run();
const toggleItalic = () => editor.value?.chain().focus().toggleItalic().run();
const toggleCode = () => editor.value?.chain().focus().toggleCode().run();

const toggleLink = () => {
    if (editor.value?.isActive('link')) {
        editor.value.chain().focus().unsetLink().run();
    } else {
        const url = window.prompt('Enter URL:');
        if (url) {
            editor.value?.chain().focus().setLink({ href: url }).run();
        }
    }
};

const toggleHeadingDropdown = () => {
    showHeadingDropdown.value = !showHeadingDropdown.value;
    showListDropdown.value = false;
    showDeleteDropdown.value = false;
};

const toggleListDropdown = () => {
    showListDropdown.value = !showListDropdown.value;
    showHeadingDropdown.value = false;
    showDeleteDropdown.value = false;
};

const toggleDeleteDropdown = () => {
    showDeleteDropdown.value = !showDeleteDropdown.value;
    showHeadingDropdown.value = false;
    showListDropdown.value = false;
};

const closeDropdowns = () => {
    showHeadingDropdown.value = false;
    showListDropdown.value = false;
    showDeleteDropdown.value = false;
};

const setHeading = (level: 1 | 2 | 3 | 4) => {
    editor.value?.chain().focus().toggleHeading({ level }).run();
    closeDropdowns();
};

const setParagraph = () => {
    editor.value?.chain().focus().setParagraph().run();
    closeDropdowns();
};

const setList = (type: ListType) => {
    if (type === 'bullet') {
        editor.value?.chain().focus().toggleBulletList().run();
    } else if (type === 'ordered') {
        editor.value?.chain().focus().toggleOrderedList().run();
    }
    closeDropdowns();
};

const toggleBlockquote = () => editor.value?.chain().focus().toggleBlockquote().run();
const toggleCodeBlock = () => editor.value?.chain().focus().toggleCodeBlock().run();
const insertHorizontalRule = () => editor.value?.chain().focus().setHorizontalRule().run();

const insertTable = () => {
    editor.value?.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run();
};

const addColumnBefore = () => editor.value?.chain().focus().addColumnBefore().run();
const addColumnAfter = () => editor.value?.chain().focus().addColumnAfter().run();
const addRowBefore = () => editor.value?.chain().focus().addRowBefore().run();
const addRowAfter = () => editor.value?.chain().focus().addRowAfter().run();

const deleteColumn = () => {
    editor.value?.chain().focus().deleteColumn().run();
    closeDropdowns();
};

const deleteRow = () => {
    editor.value?.chain().focus().deleteRow().run();
    closeDropdowns();
};

const deleteTable = () => {
    editor.value?.chain().focus().deleteTable().run();
    closeDropdowns();
};

const undo = () => editor.value?.chain().focus().undo().run();
const redo = () => editor.value?.chain().focus().redo().run();

const updateActiveStates = () => {
    if (!editor.value) return;
    canUndo.value = editor.value.can().undo() || false;
    canRedo.value = editor.value.can().redo() || false;
    isInTable.value = editor.value.isActive('table') || false;
    isBoldActive.value = editor.value.isActive('bold') || false;
    isItalicActive.value = editor.value.isActive('italic') || false;
    isCodeActive.value = editor.value.isActive('code') || false;
    isLinkActive.value = editor.value.isActive('link') || false;
    isBlockquoteActive.value = editor.value.isActive('blockquote') || false;
    isCodeBlockActive.value = editor.value.isActive('codeBlock') || false;
    isBulletListActive.value = editor.value.isActive('bulletList') || false;
    isOrderedListActive.value = editor.value.isActive('orderedList') || false;
    isParagraphActive.value = editor.value.isActive('paragraph') || false;
    isHeading1Active.value = editor.value.isActive('heading', { level: 1 }) || false;
    isHeading2Active.value = editor.value.isActive('heading', { level: 2 }) || false;
    isHeading3Active.value = editor.value.isActive('heading', { level: 3 }) || false;
    isHeading4Active.value = editor.value.isActive('heading', { level: 4 }) || false;
};

const emitUpdate = () => {
    if (!editor.value) return;

    const markdown = editor.value.storage.markdown.getMarkdown();
    
    if (markdown !== lastEmittedValue.value) {
        lastEmittedValue.value = markdown;
        emit('update:modelValue', markdown);
        
        if (props.autoSave) {
            scheduleAutoSave();
        }
    }
};

const scheduleAutoSave = () => {
    if (autoSaveTimer.value) {
        clearTimeout(autoSaveTimer.value);
    }
    
    autoSaveState.value = 'changed';
    
    autoSaveTimer.value = window.setTimeout(() => {
        triggerAutoSave();
    }, 5000);
};

const triggerAutoSave = () => {
    if (autoSaveState.value === 'changed') {
        autoSaveState.value = 'saving';
        emit('save');
    }
};

const handleSave = () => {
    emit('save');
};

const syncHeading = (newTitle: string) => {
    if (!editor.value) return;
    
    let firstH1Pos: number | null = null;
    let firstH1Node: any = null;
    
    editor.value.state.doc.descendants((node, pos) => {
        if (firstH1Pos === null && node.type.name === 'heading' && node.attrs.level === 1) {
            firstH1Pos = pos;
            firstH1Node = node;
            return false;
        }
    });

    if (firstH1Pos !== null && firstH1Node !== null) {
        const from = firstH1Pos + 1;
        const to = firstH1Pos + firstH1Node.nodeSize - 1;
        
        editor.value.chain()
            .focus()
            .setTextSelection({ from, to })
            .insertContent(newTitle)
            .run();
    }
};

// Event handlers
let handleClickOutside: ((event: MouseEvent) => void) | null = null;
let handleKeyDown: ((event: KeyboardEvent) => void) | null = null;

// Lifecycle hooks
onMounted(() => {
    handleClickOutside = (event: MouseEvent) => {
        const toolbar = (event.currentTarget as Document).querySelector('.tiptap-toolbar');
        if (toolbar && !toolbar.contains(event.target as Node)) {
            closeDropdowns();
        }
    };
    document.addEventListener('click', handleClickOutside);
    
    handleKeyDown = (event: KeyboardEvent) => {
        if ((event.ctrlKey || event.metaKey) && event.key === 's') {
            event.preventDefault();
            // Cancel any pending auto-save
            if (autoSaveTimer.value) {
                clearTimeout(autoSaveTimer.value);
                autoSaveTimer.value = null;
            }
            // Trigger immediate save if there are changes or auto-save is pending
            if (!props.isSaving && (hasChanges.value || autoSaveState.value === 'changed')) {
                if (props.autoSave) {
                    autoSaveState.value = 'saving';
                }
                handleSave();
            }
        }
    };
    document.addEventListener('keydown', handleKeyDown);
    
    if (!editorElement.value) return;
    
    editor.value = new Editor({
        element: editorElement.value,
        extensions: [
            StarterKit.configure({
                link: false
            }),
            Markdown,
            Table.configure({
                resizable: true,
            }),
            TableRow,
            TableHeader,
            TableCell,
            Link.configure({
                openOnClick: false,
                HTMLAttributes: {
                    class: 'tiptap-link'
                }
            })
        ],
        content: props.modelValue || '',
        editable: !props.readonly,
        editorProps: {
            attributes: {
                class: 'tiptap-editor-content markdown-container'
            }
        },
        onUpdate: () => {
            emitUpdate();
        },
        onTransaction: () => {
            updateActiveStates();
        }
    });
    
    originalContent.value = props.modelValue || '';
    lastEmittedValue.value = props.modelValue || '';
    updateActiveStates();
});

onBeforeUnmount(() => {
    if (handleClickOutside) {
        document.removeEventListener('click', handleClickOutside);
    }
    if (handleKeyDown) {
        document.removeEventListener('keydown', handleKeyDown);
    }
    
    if (autoSaveTimer.value) {
        clearTimeout(autoSaveTimer.value);
    }
    
    if (editor.value) {
        editor.value.destroy();
        editor.value = null;
    }
});

// Expose methods for parent components
defineExpose({
    syncHeading
});
</script>
