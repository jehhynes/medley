import { defineConfig } from 'vite';
import { resolve } from 'path';

export default defineConfig({
  build: {
    // Output to wwwroot/js/dist
    outDir: 'wwwroot/js/dist',
    emptyOutDir: true,
    
    // Library mode for building the TipTap editor as a standalone module
    lib: {
      entry: resolve(__dirname, 'src-js/tiptap-editor.js'),
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
