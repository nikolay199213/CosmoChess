# CosmoChess Deployment Guide

This document describes the production deployment process for CosmoChess.

## Architecture Overview

```
Internet → Nginx (port 80) → Frontend Container (nginx:8080)
                           → Backend Container (ASP.NET:5000)
                           → PostgreSQL (port 5432)
```

## CI/CD Pipeline

### Build Workflow (`.github/workflows/build.yml`)

Triggered on push to `main` branch:

1. Builds backend Docker image from `backend/CosmoChess.Api/Dockerfile`
2. Builds frontend Docker image from `frontend/Dockerfile`
3. Pushes images to GitHub Container Registry (GHCR)
   - `ghcr.io/nikolay199213/cosmochess-backend:latest`
   - `ghcr.io/nikolay199213/cosmochess-frontend:latest`

### Deploy Workflow (`.github/workflows/deploy.yml`)

Triggered manually or after successful build:

1. SSH into production server
2. Login to GHCR with PAT token
3. Pull latest images
4. Start containers with `docker-compose.prod.yml`
5. Restart nginx to reload config
6. Prune unused images

## Production Configuration

### Environment Variables (docker-compose.prod.yml)

**Backend:**
- `ASPNETCORE_ENVIRONMENT=Production`
- `DB_CONNECTION_STRING=Host=postgres;Database=cosmochess;Username=postgres;Password=password123`
- `JWT_KEY=b62fe252fe0b37a0efa8b9f6ec065b28`

**Note**: Update these credentials before production deployment!

### Required Secrets

Configure in GitHub repository settings:

- `GHCR_PAT` - GitHub Personal Access Token with `write:packages` permission
- `SERVER_HOST` - Production server IP/hostname
- `SERVER_USER` - SSH user
- `SERVER_SSH_KEY` - Private SSH key for deployment

## Manual Deployment

### Initial Setup

1. **Clone repository on server:**
   ```bash
   git clone https://github.com/Nikolay199213/CosmoChess.git
   cd CosmoChess
   ```

2. **Update environment variables in `docker-compose.prod.yml`:**
   - Change database password
   - Generate new JWT_KEY
   - Update any other sensitive data

3. **Login to GHCR:**
   ```bash
   echo $GHCR_PAT | docker login ghcr.io -u Nikolay199213 --password-stdin
   ```

4. **Start services:**
   ```bash
   docker compose -f docker-compose.prod.yml up -d
   ```

### Updates

1. **Pull latest code:**
   ```bash
   git pull origin main
   ```

2. **Pull and restart containers:**
   ```bash
   docker compose -f docker-compose.prod.yml pull
   docker compose -f docker-compose.prod.yml up -d
   docker restart cosmochess-nginx
   ```

3. **View logs:**
   ```bash
   docker logs cosmochess-backend
   docker logs cosmochess-frontend
   docker logs cosmochess-nginx
   ```

## Container Details

### Frontend Container
- **Image**: Built from multi-stage Dockerfile
- **Base**: `nginx:alpine`
- **Port**: 8080 (internal)
- **Content**: Static Vue.js files in `/usr/share/nginx/html`

### Backend Container
- **Image**: Built from .NET Dockerfile
- **Base**: `mcr.microsoft.com/dotnet/aspnet:8.0`
- **Port**: 5000 (internal)
- **Features**: Automatic database migrations on startup

### Nginx Container
- **Image**: `nginx:alpine`
- **Port**: 80 (public)
- **Config**: Mounted from `./nginx/nginx.conf`

### PostgreSQL Container
- **Image**: `postgres:15`
- **Port**: 5432
- **Volume**: `postgres_data` for persistence

## Network

All containers communicate via `cosmochess-network` Docker network:
- Internal DNS: `frontend`, `backend`, `postgres`
- Isolated from host network
- Only nginx port 80 exposed to internet

## Troubleshooting

### Check container status:
```bash
docker compose -f docker-compose.prod.yml ps
```

### View logs:
```bash
docker logs cosmochess-backend -f
docker logs cosmochess-nginx -f
```

### Restart specific service:
```bash
docker restart cosmochess-backend
docker restart cosmochess-nginx
```

### Test nginx config:
```bash
docker exec cosmochess-nginx nginx -t
```

### Access database:
```bash
docker exec -it cosmochess-postgres psql -U postgres -d cosmochess
```

## Security Considerations

1. **Change default passwords** in production
2. **Use strong JWT_KEY** (generate with `openssl rand -hex 32`)
3. **Enable SSL/TLS** by adding SSL configuration to nginx
4. **Restrict database access** (don't expose port 5432 publicly)
5. **Use Docker secrets** instead of environment variables for sensitive data
6. **Enable firewall** to allow only ports 80/443
7. **Regular updates** of Docker images and system packages

## SSL/TLS Configuration (Optional)

To enable HTTPS with Let's Encrypt:

1. Install certbot on host
2. Obtain certificate for your domain
3. Update `nginx/nginx.conf` to listen on port 443
4. Mount certificate files as volumes in docker-compose
5. Add automatic renewal with cron job

## Monitoring

Consider adding:
- Docker health checks
- Logging aggregation (ELK stack, Loki)
- Metrics collection (Prometheus + Grafana)
- Uptime monitoring
- Error tracking (Sentry)

## Backup

### Database Backup
```bash
docker exec cosmochess-postgres pg_dump -U postgres cosmochess > backup.sql
```

### Restore Database
```bash
docker exec -i cosmochess-postgres psql -U postgres cosmochess < backup.sql
```
