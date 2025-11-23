# Changelog

All notable changes to the CosmoChess project.

## [Unreleased]

### Added

#### Chess Bot System
- **Bot Opponents with AI Intelligence** (PR #11, #13):
  - Stockfish-powered chess bot with 6 difficulty levels (Beginner to Master)
  - Estimated ratings from 400-600 (Beginner) to 2400+ (Master)
  - Multi-PV analysis for realistic move selection
  - Difficulty-based move probability distribution
  - Realistic thinking delays (500ms to 5000ms)

- **Playing Styles** (PR #11):
  - **Aggressive**: Prefers attacks, captures, and tactical complications
  - **Balanced**: Well-rounded play without bias
  - **Solid**: Positional and defensive play, avoiding complications
  - Style strength varies by difficulty level

- **Blunder Prevention System** (PR #13):
  - Difficulty-appropriate filtering of obviously bad moves
  - Thresholds: Beginner (no filter), Easy (-300cp), Medium (-200cp), Hard (-150cp), Expert/Master (-100cp)
  - Prevents unrealistic mistakes while maintaining difficulty accuracy
  - Fallback to top 3 moves if all moves filtered

- **Capture Priority** (PR #13):
  - Detects hanging pieces worth capturing (>200cp advantage)
  - Difficulty-based capture probability (50% Beginner to 98% Master)
  - Material thresholds: minor piece (200cp), rook (500cp), queen (900cp)

- **Captured Pieces Display** (PR #12):
  - Visual display of captured pieces with Unicode symbols
  - Real-time piece count tracking
  - Sorted by piece value (queen to pawn)

- **Material Advantage Calculation** (PR #12, #13):
  - Real-time material evaluation in centipawns
  - Displayed in whole pawns with +/- indicator
  - Green highlighting for visual clarity
  - Material values: pawn (100), knight (320), bishop (330), rook (500), queen (900)

- **Bot Game API**:
  - `POST /api/games/vs-bot` - Create bot game with difficulty and style
  - `POST /api/games/analyze-multipv` - Multi-PV position analysis
  - PlayBotView.vue - Frontend UI for bot game creation
  - BotMoveBackgroundService for asynchronous bot move processing

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
  - Added comprehensive Features section with bot system details
  - Bot difficulty levels and playing styles overview
  - Captured pieces and material advantage features
  - Production deployment section
  - Updated architecture details with specific ports
  - Added CI/CD information
  - Added links to all documentation files

- **backend/README.md** - Backend documentation improvements:
  - **Bot System Architecture section** with component details:
    - StockfishEngine lifecycle and UCI protocol
    - BotService difficulty levels, styles, and algorithms
    - BotMoveBackgroundService async processing
    - Complete bot game flow diagram
  - Updated API endpoints with bot endpoints and request/response formats
  - Analysis endpoints (analyze and analyze-multipv)
  - Production configuration section
  - Environment variables reference
  - Configuration notes with code examples

- **frontend/README.md** - Frontend documentation improvements:
  - Bot game features and UI components
  - Captured pieces display and material advantage
  - PlayBotView component documentation
  - PlayerInfo component with material advantage display
  - Material calculation utilities (capturedPieces.js)
  - Updated API integration section with bot endpoints
  - Components overview with new views and utilities
  - Updated Docker deployment section
  - Added SignalR connection details
  - Nginx configuration split (frontend container vs main proxy)
  - Fixed port numbers (8080 instead of 80)

- **CHANGELOG.md** - Comprehensive bot system documentation:
  - Chess bot system features and implementation details
  - Difficulty levels with thresholds and behavior
  - Playing styles with characteristics
  - Blunder prevention algorithm
  - Capture priority system
  - Material advantage calculation

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
