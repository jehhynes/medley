import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'path';

export default defineConfig({
  plugins: [vue()],
  
  build: {
    // Output to wwwroot/js/dist
    outDir: 'wwwroot/js/dist',
    emptyOutDir: true,
    
    // Library mode for building all components as a bundle
    lib: {
      entry: resolve(__dirname, 'src-js/main.js'),
      name: 'MedleyComponents',
      formats: ['iife'],
      fileName: () => 'components.js'
    },
    
    // Externalize Vue since it's already loaded globally
    rollupOptions: {
      external: ['vue'],
      output: {
        globals: {
          vue: 'Vue'
        },
        // Ensure all exports are available on window
        extend: true
      }
    },
    
    // Generate sourcemaps for debugging
    sourcemap: true,
    
    // Minify in production
    minify: 'terser',
    
    // Optimize chunk size
    chunkSizeWarningLimit: 1000
  },
  
  // Resolve configuration
  resolve: {
    alias: {
      vue: 'vue/dist/vue.esm-bundler.js',
      '@': resolve(__dirname, 'src-js')
    }
  },
  
  // Development server configuration
  server: {
    hmr: {
      protocol: 'ws',
      host: 'localhost'
    }
  }
});
