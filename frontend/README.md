# CosmoChess Frontend

Vue.js frontend for the CosmoChess platform with nginx, Vue.js, and custom chessboard implementation.

## Features

- **Authentication**: Login and registration pages with JWT token management
- **Games List**: View available games waiting for players to join
- **Chessboard**: Interactive chessboard for playing chess games
- **Game Management**: Create new games, join existing games, and make moves
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
- Backend API on port 5000
- Frontend on port 8080

### Frontend Only

```bash
cd frontend
docker build -t cosmochess-frontend .
docker run -p 8080:80 cosmochess-frontend
```

## API Integration

The frontend communicates with the backend API through the following endpoints:

- `POST /api/auth/login` - User authentication
- `POST /api/auth/register` - User registration
- `GET /api/games/wait-join` - Get games waiting for players
- `POST /api/games/create` - Create a new game
- `POST /api/games/join` - Join an existing game
- `POST /api/games/move` - Make a move in a game
- `POST /api/games/analyze` - Analyze a chess position

## Components Overview

### Views
- **LoginView**: Authentication page with login/register forms
- **GamesView**: List of available games and user's created games
- **GameView**: Interactive chessboard for playing games

### Services
- **authService**: Handles authentication, token management, and user session
- **gameService**: Manages game operations like creating, joining, and making moves

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

The production build uses nginx with:
- Static file serving with caching
- API proxy to backend
- CORS headers for development
- Security headers
- Gzip compression

## Development Notes

- The chessboard uses vue3-chessboard library (based on Chessground from Lichess)
- Chess logic is handled by Chess.js library
- Authentication tokens are stored in localStorage
- API requests include automatic token injection via axios interceptors
- The application includes route guards for authenticated routes
- Chess.js instance is marked as non-reactive (markRaw) to prevent performance issues