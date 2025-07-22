import vue from "@vitejs/plugin-vue";
import path from 'path';
import { defineConfig } from "vite";

export default defineConfig({
    resolve: {
    alias: {
      // Explicitly alias to Vue 3's ESM bundler build
      vue: 'vue/dist/vue.esm-bundler.js',
    },
  },
    plugins: [vue() as any],
    define: {
        // Define the environment variables Vue expects
        'process.env.NODE_ENV': JSON.stringify(process.env.NODE_ENV || 'production'),
        'process.env.DEV': process.env.NODE_ENV !== 'production',
        'process.env.PROD': process.env.NODE_ENV === 'production',
        // If you're using Vue 3's feature flags, you might need these:
        '__VUE_OPTIONS_API__': JSON.stringify(true),
        '__VUE_PROD_DEVTOOLS__': JSON.stringify(false),
    },
    build: {
        lib: {
            entry: path.resolve(__dirname, 'src/main.ts'),
            name: 'QuillEditor',
            formats: ['umd'], // UMD format will make it globally available
            fileName: (format) => `quill-editor.${format}.js`
        },
        outDir: path.resolve(__dirname, '../../wwwroot/QuillEditor'),
        rollupOptions: {
            // Make sure to externalize deps that shouldn't be bundled
            external: [],
            output: {
                // Provide global variables to use in the UMD build
                globals: {
                    vue: 'Vue'
                },
                // This ensures your component is attached to the window object
                exports: 'named'
            }
        }
    }
});
