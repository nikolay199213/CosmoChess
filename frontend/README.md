# CosmoChess Frontend

Vue.js frontend for the CosmoChess platform with nginx, Vue.js, and custom chessboard implementation.

## Features

- **Authentication**: Login and registration pages with JWT token management
- **Multiplayer Games**: View and join games waiting for players
- **AI Bot Games**: Play against configurable chess bots
  - Select from 6 difficulty levels (Beginner to Master)
  - Choose playing style (Aggressive, Balanced, Solid)
  - Configure time controls
- **Interactive Chessboard**: Professional drag-and-drop chessboard for playing games
- **Game Analysis Features**:
  - Captured pieces display with piece counts
  - Real-time material advantage calculation (in pawns)
  - Visual indicators for who is ahead
- **Position Analysis**: Integration with Stockfish engine for position analysis

## Technology Stack

- **Vue.js 3**: Frontend framework
- **Vue Router**: Client-side routing
- **Axios**: HTTP client for API communication
- **Chess.js**: Chess logic and move validation
- **vue3-chessboard**: Professional chessboard component based on Chessground
- **Nginx**: Web server for production deployment

## Development Setup

### Prerequisites

- Node.js 18+ and npm
- Backend API running on port 5000

### Installation

1. Navigate to the frontend directory:
   ```bash
   cd frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm run dev
   ```

The application will be available at `http://localhost:8080`

### Build for Production

```bash
npm run build
```

## Docker Deployment

### Using Docker Compose (Recommended)

From the root directory:

```bash
docker-compose up --build
```

This will start:
- PostgreSQL database on port 5432
- Backend API (internal port 5000)
- Frontend nginx server (internal port 8080)
- Nginx reverse proxy on port 80

The application will be accessible at `http://localhost`

### Production Build

The production Dockerfile uses a multi-stage build:
1. **Build stage**: Node.js builds the Vue.js application
2. **Production stage**: Nginx serves the static files on port 8080

```bash
cd frontend
docker build -t cosmochess-frontend .
docker run -p 8080:8080 cosmochess-frontend
```

## API Integration

The frontend communicates with the backend API through the following endpoints:

### Authentication
- `POST /api/auth/login` - User authentication
- `POST /api/auth/register` - User registration

### Game Management
- `GET /api/games/wait-join` - Get games waiting for players
- `POST /api/games/create` - Create a new multiplayer game
- `POST /api/games/vs-bot` - Create a new bot game with difficulty and style
- `POST /api/games/join` - Join an existing game
- `POST /api/games/move` - Make a move in a game

### Analysis
- `POST /api/games/analyze` - Analyze a chess position with Stockfish
- `POST /api/games/analyze-multipv` - Get multiple principal variations from Stockfish

### SignalR Real-time Connection

- WebSocket endpoint: `/api/gamehub`
- Used for real-time game updates and player moves

## Components Overview

### Views
- **LoginView**: Authentication page with login/register forms
- **GamesView**: List of available games and user's created games
- **PlayBotView**: Bot game creation with difficulty and style selection
- **GameView**: Interactive chessboard for playing games

### Components
- **PlayerInfo.vue**: Displays player name, captured pieces, and material advantage
- **Chessboard**: Interactive chessboard with drag-and-drop and move highlighting

### Services
- **authService**: Handles authentication, token management, and user session
- **gameService**: Manages game operations like creating, joining, and making moves

### Utilities
- **capturedPieces.js**: Material advantage calculations and captured pieces tracking
  - Piece valuation (pawn=100, knight=320, bishop=330, rook=500, queen=900)
  - FEN parsing and piece counting
  - Material advantage computation

### Features
- **JWT Authentication**: Secure authentication with automatic token management
- **Responsive Design**: Works on desktop and mobile devices
- **Real-time Updates**: Game list refreshes automatically
- **Move Validation**: Client-side and server-side move validation
- **Position Analysis**: Integration with Stockfish engine for game analysis

## Configuration

### Environment Variables

The application can be configured through environment variables or by modifying the services:

- Backend API URL: Currently proxied through `/api` to `http://localhost:5000`
- Update `vite.config.js` to change the proxy configuration

### Nginx Configuration

#### Frontend Container (port 8080)
The production Dockerfile configures nginx to:
- Serve static files from `/usr/share/nginx/html`
- Handle Vue Router with `try_files` for SPA support
- Listen on port 8080

#### Main Nginx Reverse Proxy (port 80)
Located at `nginx/nginx.conf`, routes requests to:
- `/` → Frontend container (port 8080)
- `/api/` → Backend container (port 5000)
- `/api/gamehub` → SignalR WebSocket hub with proper WebSocket headers

## Development Notes

- The chessboard uses vue3-chessboard library (based on Chessground from Lichess)
- Chess logic is handled by Chess.js library
- Authentication tokens are stored in localStorage
- API requests include automatic token injection via axios interceptors
- The application includes route guards for authenticated routes
- Chess.js instance is marked as non-reactive (markRaw) to prevent performance issues