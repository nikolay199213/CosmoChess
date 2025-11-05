# Changelog

All notable changes to the CosmoChess project.

## [Unreleased]

### Added

#### Documentation
- **CONFIGURATION.md** - Comprehensive configuration reference with:
  - Critical configuration points (SignalR paths, middleware order, HTTPS redirection)
  - Common mistakes to avoid with examples
  - Environment-specific behavior comparison table
  - Troubleshooting checklist

- **DEPLOYMENT.md** - Complete deployment guide covering:
  - Architecture overview
  - CI/CD pipeline details
  - Manual deployment instructions
  - Container details and network configuration
  - Security considerations
  - SSL/TLS setup guide
  - Backup and restore procedures

- **nginx/README.md** - Nginx configuration documentation with:
  - Routing rules for all endpoints
  - Buffer configuration explanation
  - WebSocket-specific settings
  - Testing commands

#### Documentation Updates
- **README.md** - Enhanced main documentation:
  - Added production deployment section
  - Updated architecture details with specific ports
  - Added CI/CD information
  - Added links to all documentation files

- **backend/README.md** - Backend documentation improvements:
  - Production configuration section
  - Environment variables reference
  - API endpoints list
  - Configuration notes with code examples

- **frontend/README.md** - Frontend documentation improvements:
  - Updated Docker deployment section
  - Added SignalR connection details
  - Nginx configuration split (frontend container vs main proxy)
  - Fixed port numbers (8080 instead of 80)

### Fixed

#### Backend Configuration
- **Program.cs**: Disabled HTTPS redirection in production (nginx handles SSL)
- **Program.cs**: Ensured correct middleware order (UseAuthentication before UseAuthorization)
- **Program.cs**: Fixed SignalR hub path to `/api/gamehub` (matches nginx configuration)

#### Nginx Configuration
- **nginx.conf**: Removed WebSocket upgrade headers from `/` and `/api/` locations
- **nginx.conf**: WebSocket headers now only applied to `/api/gamehub` endpoint
- **nginx.conf**: Increased buffer sizes to prevent connection resets
- **nginx.conf**: Added proper proxy settings with correct timeouts

#### Docker Configuration
- **docker-compose.prod.yml**: Added `JWT_KEY` environment variable
- **docker-compose.prod.yml**: Changed to use `DB_CONNECTION_STRING` instead of `ConnectionStrings__DefaultConnection`
- **docker-compose.prod.yml**: Fixed frontend port exposure (8080 internal)

#### Frontend Build
- **Dockerfile**: Simplified to use nginx:alpine directly (multi-stage build)
- **Dockerfile**: Configured nginx to listen on port 8080
- **Dockerfile**: Added SPA routing support with try_files

### Changed

#### Architecture
- Migrated from shared volume approach to nginx-to-nginx proxy pattern
- Frontend container now runs its own nginx on port 8080
- Main nginx proxies to frontend:8080 instead of serving files directly
- All services communicate via Docker network with internal DNS

#### CI/CD
- **deploy.yml**: Added nginx restart after deployment
- **build.yml**: Confirmed correct Dockerfile paths

## Production Deployment - 2025-01-XX

### Summary
Successfully deployed CosmoChess to production server at http://cosmochess.ru (195.133.49.16)

### Issues Resolved
1. Fixed ERR_CONNECTION_RESET caused by incorrect WebSocket headers
2. Resolved middleware order issues in backend
3. Corrected SignalR hub path inconsistencies
4. Fixed environment variable naming in Docker Compose
5. Identified and documented GoodbyeDPI client-side interference issue

### Lessons Learned
- WebSocket upgrade headers should only be on WebSocket endpoints
- Client-side network filtering software can cause mysterious connection issues
- Nginx buffer sizes matter for preventing connection resets
- Environment variable naming is different between appsettings.json and Docker
- Testing from multiple clients/networks is important for diagnosis

## Development Setup - 2025-01-XX

### Initial Setup
- Created multi-container Docker architecture
- Set up PostgreSQL database with automatic migrations
- Configured JWT authentication
- Integrated Stockfish chess engine
- Implemented SignalR for real-time gameplay
- Set up GitHub Actions for CI/CD
