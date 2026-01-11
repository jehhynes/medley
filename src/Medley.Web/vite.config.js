import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'path';

export default defineConfig({
  plugins: [vue()],
  
  build: {
    // Output to wwwroot/js/dist
    outDir: 'wwwroot/js/dist',
    emptyOutDir: true,
    
    // Library mode for building the TipTap editor as a standalone module
    lib: {
      entry: resolve(__dirname, 'src-js/TiptapEditor.vue'),
      name: 'TiptapEditor',
      formats: ['iife'],
      fileName: () => 'tiptap-editor.js'
    },
    
    // Externalize Vue since it's already loaded globally
    rollupOptions: {
      external: ['vue'],
      output: {
        globals: {
          vue: 'Vue'
        }
      }
    },
    
    // Generate sourcemaps for debugging
    sourcemap: true,
    
    // Minify in production
    minify: 'terser'
  },
  
  // Resolve configuration
  resolve: {
    alias: {
      vue: 'vue/dist/vue.esm-bundler.js'
    }
  }
});
