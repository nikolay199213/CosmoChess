# Nginx Reverse Proxy Configuration

This directory contains the nginx configuration for the CosmoChess application reverse proxy.

## Overview

Nginx acts as the entry point for all requests and routes them to the appropriate backend services:

- **Frontend**: Vue.js application served by nginx (port 8080)
- **Backend API**: .NET application (port 5000)
- **SignalR Hub**: WebSocket connection for real-time gameplay

## Routing Configuration

### Frontend (`/`)
```nginx
location / {
    proxy_pass http://frontend:8080;
}
```
Routes all requests to the frontend container where nginx serves the static Vue.js application.

### Backend API (`/api/`)
```nginx
location /api/ {
    proxy_pass http://backend:5000/api/;
}
```
Routes API requests to the backend .NET application with standard HTTP headers.

### SignalR WebSocket (`/api/gamehub`)
```nginx
location /api/gamehub {
    proxy_pass http://backend:5000/api/gamehub;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_buffering off;
}
```
Special configuration for WebSocket connections with:
- Upgrade headers for WebSocket protocol
- Disabled buffering for real-time communication
- Extended timeouts (7 days) for long-lived connections

## Buffer Configuration

```nginx
proxy_buffer_size 128k;
proxy_buffers 8 128k;
proxy_busy_buffers_size 256k;
client_max_body_size 50M;
```

Increased buffer sizes prevent connection resets and support large requests.

## Health Check

```nginx
location /health {
    return 200 "OK\n";
}
```

Simple health check endpoint that returns 200 OK without logging.

## Important Notes

- **WebSocket headers** are ONLY applied to `/api/gamehub` endpoint
- Standard HTTP locations (`/` and `/api/`) do NOT have WebSocket upgrade headers
- All proxy requests use HTTP/1.1 protocol
- Services communicate within the `cosmochess-network` Docker network

## Testing

Test the configuration:
```bash
# Test config syntax
docker exec cosmochess-nginx nginx -t

# Reload config
docker exec cosmochess-nginx nginx -s reload

# View logs
docker logs cosmochess-nginx
```
