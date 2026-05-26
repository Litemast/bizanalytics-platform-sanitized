import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

const apiProxy = {
  "/api": {
    target: "http://127.0.0.1:5000",
    changeOrigin: true
  }
};

export default defineConfig({
  plugins: [react()],
  server: {
    host: true,
    port: 5173,
    allowedHosts: true,
    proxy: apiProxy
  },
  preview: {
    host: true,
    port: 5173,
    allowedHosts: true,
    proxy: apiProxy
  }
});
