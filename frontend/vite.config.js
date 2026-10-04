import { defineConfig, loadEnv } from 'vite';

function apiProxy(target) {
  return {
    target,
    changeOrigin: true,
    secure: false,
    configure(proxy) {
      proxy.on('error', (_error, _request, response) => {
        if (!response.headersSent && typeof response.writeHead === 'function') {
          response.writeHead(503, { 'Content-Type': 'application/problem+json' });
          response.end(JSON.stringify({
            title: 'Service unavailable',
            detail: 'The service is temporarily unavailable. Please try again later.',
          }));
        }
      });
    },
  };
}

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const target = env.API_PROXY_TARGET || 'https://localhost:65408';
  return {
    server: { port: 5173, strictPort: true, proxy: {
      '/api': apiProxy(target),
      '/health': apiProxy(target),
      '/uploads': apiProxy(target),
    } },
  };
});
