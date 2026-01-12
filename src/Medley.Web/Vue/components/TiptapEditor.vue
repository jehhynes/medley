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

<script>
import { Editor } from '@tiptap/vue-3';
import StarterKit from '@tiptap/starter-kit';
import { Table } from '@tiptap/extension-table';
import TableRow from '@tiptap/extension-table-row';
import TableCell from '@tiptap/extension-table-cell';
import TableHeader from '@tiptap/extension-table-header';
import { Markdown } from 'tiptap-markdown';
import Link from '@tiptap/extension-link';

export default {
    name: 'TiptapEditor',
    props: {
        modelValue: {
            type: String,
            default: ''
        },
        isSaving: {
            type: Boolean,
            default: false
        },
        autoSave: {
            type: Boolean,
            default: false
        },
        showSaveButton: {
            type: Boolean,
            default: true
        },
        showFormatting: {
            type: Boolean,
            default: true
        },
        readonly: {
            type: Boolean,
            default: false
        }
    },
    emits: ['update:modelValue', 'save'],
    data() {
        return {
            editor: null,
            canUndo: false,
            canRedo: false,
            isInTable: false,
            lastEmittedValue: '',
            originalContent: '',
            showHeadingDropdown: false,
            showListDropdown: false,
            showDeleteDropdown: false,
            autoSaveTimer: null,
            autoSaveState: 'saved',
            isBoldActive: false,
            isItalicActive: false,
            isCodeActive: false,
            isLinkActive: false,
            isBlockquoteActive: false,
            isCodeBlockActive: false,
            isBulletListActive: false,
            isOrderedListActive: false,
            isParagraphActive: false,
            isHeading1Active: false,
            isHeading2Active: false,
            isHeading3Active: false,
            isHeading4Active: false
        };
    },
    computed: {
        isHeadingActive() {
            return this.isHeading1Active || this.isHeading2Active || 
                   this.isHeading3Active || this.isHeading4Active;
        },
        isListActive() {
            return this.isBulletListActive || this.isOrderedListActive;
        },
        hasChanges() {
            return this.lastEmittedValue !== this.originalContent;
        },
        autoSaveIndicatorText() {
            if (this.autoSaveState === 'saving' || this.autoSaveState === 'changed') {
                return 'Saving...';
            }
            return 'Saved';
        },
        autoSaveIconClass() {
            if (this.autoSaveState === 'saving' || this.autoSaveState === 'changed') {
                return 'far fa-arrows-rotate fa-spin';
            }
            return 'far fa-cloud-check';
        }
    },
    watch: {
        modelValue: {
            handler(newValue, oldValue) {
                if (!this.editor || this.editor.isDestroyed) return;
                if (this.lastEmittedValue === (newValue || '')) return;

                this.editor.commands.setContent(newValue || '', false);
                this.originalContent = newValue || '';
                this.lastEmittedValue = newValue || '';
            }
        },
        isSaving: {
            handler(newValue, oldValue) {
                if (oldValue === true && newValue === false) {
                    this.originalContent = this.lastEmittedValue;
                    if (this.autoSave) {
                        this.autoSaveState = 'saved';
                    }
                }
            }
        },
        readonly: {
            handler(newValue) {
                if (this.editor && !this.editor.isDestroyed) {
                    this.editor.setEditable(!newValue);
                }
            }
        }
    },
    methods: {
        toggleBold() { this.editor?.chain().focus().toggleBold().run(); },
        toggleItalic() { this.editor?.chain().focus().toggleItalic().run(); },
        toggleCode() { this.editor?.chain().focus().toggleCode().run(); },
        
        toggleLink() {
            if (this.editor?.isActive('link')) {
                this.editor.chain().focus().unsetLink().run();
            } else {
                const url = window.prompt('Enter URL:');
                if (url) {
                    this.editor?.chain().focus().setLink({ href: url }).run();
                }
            }
        },
        
        toggleHeadingDropdown() {
            this.showHeadingDropdown = !this.showHeadingDropdown;
            this.showListDropdown = false;
            this.showDeleteDropdown = false;
        },
        toggleListDropdown() {
            this.showListDropdown = !this.showListDropdown;
            this.showHeadingDropdown = false;
            this.showDeleteDropdown = false;
        },
        toggleDeleteDropdown() {
            this.showDeleteDropdown = !this.showDeleteDropdown;
            this.showHeadingDropdown = false;
            this.showListDropdown = false;
        },
        closeDropdowns() {
            this.showHeadingDropdown = false;
            this.showListDropdown = false;
            this.showDeleteDropdown = false;
        },
        
        setHeading(level) {
            this.editor?.chain().focus().toggleHeading({ level }).run();
            this.closeDropdowns();
        },
        setParagraph() {
            this.editor?.chain().focus().setParagraph().run();
            this.closeDropdowns();
        },
        
        setList(type) {
            if (type === 'bullet') {
                this.editor?.chain().focus().toggleBulletList().run();
            } else if (type === 'ordered') {
                this.editor?.chain().focus().toggleOrderedList().run();
            }
            this.closeDropdowns();
        },
        
        toggleBlockquote() { this.editor?.chain().focus().toggleBlockquote().run(); },
        toggleCodeBlock() { this.editor?.chain().focus().toggleCodeBlock().run(); },
        insertHorizontalRule() { this.editor?.chain().focus().setHorizontalRule().run(); },

        insertTable() {
            this.editor?.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run();
        },
        addColumnBefore() { this.editor?.chain().focus().addColumnBefore().run(); },
        addColumnAfter() { this.editor?.chain().focus().addColumnAfter().run(); },
        addRowBefore() { this.editor?.chain().focus().addRowBefore().run(); },
        addRowAfter() { this.editor?.chain().focus().addRowAfter().run(); },
        deleteColumn() { 
            this.editor?.chain().focus().deleteColumn().run(); 
            this.closeDropdowns();
        },
        deleteRow() { 
            this.editor?.chain().focus().deleteRow().run(); 
            this.closeDropdowns();
        },
        deleteTable() { 
            this.editor?.chain().focus().deleteTable().run(); 
            this.closeDropdowns();
        },

        undo() { this.editor?.chain().focus().undo().run(); },
        redo() { this.editor?.chain().focus().redo().run(); },

        updateActiveStates() {
            if (!this.editor) return;
            this.canUndo = this.editor.can().undo() || false;
            this.canRedo = this.editor.can().redo() || false;
            this.isInTable = this.editor.isActive('table') || false;
            this.isBoldActive = this.editor.isActive('bold') || false;
            this.isItalicActive = this.editor.isActive('italic') || false;
            this.isCodeActive = this.editor.isActive('code') || false;
            this.isLinkActive = this.editor.isActive('link') || false;
            this.isBlockquoteActive = this.editor.isActive('blockquote') || false;
            this.isCodeBlockActive = this.editor.isActive('codeBlock') || false;
            this.isBulletListActive = this.editor.isActive('bulletList') || false;
            this.isOrderedListActive = this.editor.isActive('orderedList') || false;
            this.isParagraphActive = this.editor.isActive('paragraph') || false;
            this.isHeading1Active = this.editor.isActive('heading', { level: 1 }) || false;
            this.isHeading2Active = this.editor.isActive('heading', { level: 2 }) || false;
            this.isHeading3Active = this.editor.isActive('heading', { level: 3 }) || false;
            this.isHeading4Active = this.editor.isActive('heading', { level: 4 }) || false;
        },

        emitUpdate() {
            if (!this.editor) return;

            const markdown = this.editor.storage.markdown.getMarkdown();
            
            if (markdown !== this.lastEmittedValue) {
                this.lastEmittedValue = markdown;
                this.$emit('update:modelValue', markdown);
                
                if (this.autoSave) {
                    this.scheduleAutoSave();
                }
            }
        },

        scheduleAutoSave() {
            if (this.autoSaveTimer) {
                clearTimeout(this.autoSaveTimer);
            }
            
            this.autoSaveState = 'changed';
            
            this.autoSaveTimer = setTimeout(() => {
                this.triggerAutoSave();
            }, 5000);
        },

        triggerAutoSave() {
            if (this.autoSaveState === 'changed') {
                this.autoSaveState = 'saving';
                this.$emit('save');
            }
        },

        handleSave() {
            this.$emit('save');
        },

        syncHeading(newTitle) {
            if (!this.editor) return;
            
            let firstH1Pos = null;
            let firstH1Node = null;
            
            this.editor.state.doc.descendants((node, pos) => {
                if (firstH1Pos === null && node.type.name === 'heading' && node.attrs.level === 1) {
                    firstH1Pos = pos;
                    firstH1Node = node;
                    return false;
                }
            });

            if (firstH1Pos !== null && firstH1Node !== null) {
                const from = firstH1Pos + 1;
                const to = firstH1Pos + firstH1Node.nodeSize - 1;
                
                this.editor.chain()
                    .focus()
                    .setTextSelection({ from, to })
                    .insertContent(newTitle)
                    .run();
            }
        }
    },
    mounted() {
        console.log('TiptapEditor showFormatting prop:', this.showFormatting);
        
        this.handleClickOutside = (event) => {
            const toolbar = this.$el.querySelector('.tiptap-toolbar');
            if (toolbar && !toolbar.contains(event.target)) {
                this.closeDropdowns();
            }
        };
        document.addEventListener('click', this.handleClickOutside);
        
        this.handleKeyDown = (event) => {
            if ((event.ctrlKey || event.metaKey) && event.key === 's') {
                event.preventDefault();
                if (!this.isSaving && this.hasChanges) {
                    this.handleSave();
                }
            }
        };
        document.addEventListener('keydown', this.handleKeyDown);
        
        this.editor = new Editor({
            element: this.$refs.editorElement,
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
            content: this.modelValue || '',
            editable: !this.readonly,
            editorProps: {
                attributes: {
                    class: 'tiptap-editor-content markdown-container'
                }
            },
            onUpdate: () => {
                this.emitUpdate();
            },
            onTransaction: () => {
                this.updateActiveStates();
            }
        });
        
        this.originalContent = this.modelValue || '';
        this.lastEmittedValue = this.modelValue || '';
        this.updateActiveStates();
    },
    beforeUnmount() {
        document.removeEventListener('click', this.handleClickOutside);
        document.removeEventListener('keydown', this.handleKeyDown);
        
        if (this.autoSaveTimer) {
            clearTimeout(this.autoSaveTimer);
        }
        
        if (this.editor) {
            this.editor.destroy();
            this.editor = null;
        }
    }
};
</script>
