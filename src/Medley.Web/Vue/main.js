// Main entry point for all Vue components
// This file imports and registers all components globally

// Import third-party libraries
import bootbox from 'bootbox';
import { marked } from 'marked';
import '@alenaksu/json-viewer';

// Import all components
import TiptapEditor from './components/TiptapEditor.vue';
import ArticleList from './components/ArticleList.vue';
import ArticleTree from './components/ArticleTree.vue';
import ChatPanel from './components/ChatPanel.vue';
import FragmentList from './components/FragmentList.vue';
import FragmentModal from './components/FragmentModal.vue';
import PlanViewer from './components/PlanViewer.vue';
import SourceList from './components/SourceList.vue';
import VerticalMenu from './components/VerticalMenu.vue';
import VersionsPanel from './components/VersionsPanel.vue';
import VirtualScroller from './components/VirtualScroller.vue';

// Import page-level SFCs
import AiPrompts from './pages/AiPrompts.vue';
import Articles from './pages/Articles.vue';
import Fragments from './pages/Fragments.vue';
import Sources from './pages/Sources.vue';

// Import all mixins
import dropdownMixin from './mixins/dropdown.js';
import articleFilterMixin from './mixins/articleFilter.js';
import articleModalMixin from './mixins/articleModal.js';
import articleSignalRMixin from './mixins/articleSignalR.js';
import articleVersionMixin from './mixins/articleVersion.js';
import infiniteScrollMixin from './mixins/infiniteScroll.js';

// Export all components for individual imports
export {
  TiptapEditor,
  ArticleList,
  ArticleTree,
  ChatPanel,
  FragmentList,
  FragmentModal,
  PlanViewer,
  SourceList,
  VerticalMenu,
  VersionsPanel,
  VirtualScroller,
  AiPrompts,
  Articles,
  Fragments,
  Sources
};

// Export all mixins for individual imports
export {
  dropdownMixin,
  articleFilterMixin,
  articleModalMixin,
  articleSignalRMixin,
  articleVersionMixin,
  infiniteScrollMixin
};

// Make components and mixins available globally on window object
if (typeof window !== 'undefined') {
  // Expose third-party libraries globally
  window.bootbox = bootbox;
  window.marked = marked;
  
  // Expose individual components
  window.TiptapEditor = TiptapEditor;
  window.ArticleList = ArticleList;
  window.ArticleTree = ArticleTree;
  window.ChatPanel = ChatPanel;
  window.FragmentList = FragmentList;
  window.FragmentModal = FragmentModal;
  window.PlanViewer = PlanViewer;
  window.SourceList = SourceList;
  window.VerticalMenu = VerticalMenu;
  window.VersionsPanel = VersionsPanel;
  window.VirtualScroller = VirtualScroller;
  
  // Expose page-level SFCs
  window.AiPrompts = AiPrompts;
  window.Articles = Articles;
  window.Fragments = Fragments;
  window.Sources = Sources;
  
  // Expose mixins for use in legacy page apps
  window.dropdownMixin = dropdownMixin;
  window.articleFilterMixin = articleFilterMixin;
  window.articleModalMixin = articleModalMixin;
  window.articleSignalRMixin = articleSignalRMixin;
  window.articleVersionMixin = articleVersionMixin;
  window.infiniteScrollMixin = infiniteScrollMixin;
}
