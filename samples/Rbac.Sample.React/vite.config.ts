import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

export default defineConfig({
  plugins: [react()],
  resolve: {
    // The library is compiled from source. Pin React to this app so Vite does
    // not pick up a second copy from packages/rbac-react/node_modules (hooks then crash).
    dedupe: ["react", "react-dom", "react-router", "react-router-dom"],
    alias: {
      "@general-rbac/react": path.resolve(import.meta.dirname, "../../packages/rbac-react/src/index.ts"),
      react: path.resolve(import.meta.dirname, "node_modules/react"),
      "react-dom": path.resolve(import.meta.dirname, "node_modules/react-dom"),
    },
  },
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: "http://localhost:5265",
        changeOrigin: true,
      },
    },
  },
});
