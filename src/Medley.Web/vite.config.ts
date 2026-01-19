import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'path';

export default defineConfig({
    plugins: [vue({
        template: {
            compilerOptions: {
                isCustomElement: (tag) => tag === 'json-viewer'
            }
        },
    })],
    
  base: '/dist/',
  
  build: {
    outDir: 'wwwroot/dist',
    emptyOutDir: true,
    
    rollupOptions: {
      input: {
        main: resolve(__dirname, 'Vue/app/main.ts')
      },
      output: {
        entryFileNames: 'js/[name].js',
        chunkFileNames: 'js/[name]-[hash].js',
        assetFileNames: (assetInfo) => {
          if (assetInfo.name && assetInfo.name.endsWith('.css')) {
            return 'css/[name].[ext]';
          }
          return 'assets/[name]-[hash].[ext]';
        }
      }
    },
    
    sourcemap: true,
    minify: 'terser',
    chunkSizeWarningLimit: 1000
  },
  
  resolve: {
    alias: {
      '@': resolve(__dirname, 'Vue')
    }
  },
  
  esbuild: {
    target: 'es2022'
  }
});
