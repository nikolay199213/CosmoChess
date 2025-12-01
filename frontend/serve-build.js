// Simple static server for production build
const express = require('express');
const { createProxyMiddleware } = require('http-proxy-middleware');
const path = require('path');

const app = express();
const PORT = 8080;

// Proxy API requests to backend
app.use('/api', createProxyMiddleware({
  target: 'http://localhost:5000',
  changeOrigin: true,
  ws: true, // WebSocket support for SignalR
  logLevel: 'info',
}));

// Serve static files from dist folder
app.use(express.static(path.join(__dirname, 'dist')));

// Fallback to index.html for SPA routing
app.get('*', (req, res) => {
  res.sendFile(path.join(__dirname, 'dist', 'index.html'));
});

app.listen(PORT, '0.0.0.0', () => {
  console.log(`✅ Production server running on http://0.0.0.0:${PORT}`);
  console.log(`📡 Proxying /api/* to http://localhost:5000`);
});
