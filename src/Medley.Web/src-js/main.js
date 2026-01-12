// Main entry point for all Vue components
// This file imports and registers all components globally

// Import TiptapEditor (already converted)
import TiptapEditor from './components/TiptapEditor.vue';

// Import all converted components
import ArticleList from './components/ArticleList.vue';
import ArticleTree from './components/ArticleTree.vue';
import ChatPanel from './components/ChatPanel.vue';
import FragmentList from './components/FragmentList.vue';
import FragmentModal from './components/FragmentModal.vue';
import PlanViewer from './components/PlanViewer.vue';
import SourceList from './components/SourceList.vue';
import VersionsPanel from './components/VersionsPanel.vue';
import VirtualScroller from './components/VirtualScroller.vue';

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
  VersionsPanel,
  VirtualScroller
};

// Make components available globally on window object
if (typeof window !== 'undefined') {
  // Expose individual components
  window.TiptapEditor = TiptapEditor;
  window.ArticleList = ArticleList;
  window.ArticleTree = ArticleTree;
  window.ChatPanel = ChatPanel;
  window.FragmentList = FragmentList;
  window.FragmentModal = FragmentModal;
  window.PlanViewer = PlanViewer;
  window.SourceList = SourceList;
  window.VersionsPanel = VersionsPanel;
  window.VirtualScroller = VirtualScroller;
}
