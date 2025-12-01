# CosmoChess

CosmoChess is a full-stack web application for playing chess in real-time. It includes user authentication, multiplayer games, AI bot opponents with configurable difficulty and playing styles, and position analysis using the Stockfish chess engine.

## Features

- **User Authentication**: Secure JWT-based authentication with login and registration
- **Multiplayer Chess**: Create and join games to play against other players in real-time via WebSockets
- **AI Bot Opponents**: Play against intelligent chess bots powered by Stockfish
  - **6 Difficulty Levels**: Beginner (400-600), Easy (800-1000), Medium (1200-1400), Hard (1600-1800), Expert (2000-2200), Master (2400+)
  - **3 Playing Styles**: Aggressive (prefers attacks & tactics), Balanced (well-rounded play), Solid (positional & defensive)
  - **Smart Move Selection**: Difficulty-based randomization with blunder prevention
  - **Realistic Behavior**: Bots miss captures and make mistakes appropriate to their level
- **Interactive Chessboard**: Professional drag-and-drop interface with legal move highlighting
- **Game Analysis**: Captured pieces display with real-time material advantage calculation
- **Position Analysis**: Stockfish integration for analyzing chess positions

## Technology & Architecture

The project consists of several key components orchestrated using Docker.

### Backend

- **Framework:** .NET / C#
- **Architecture:** Follows Clean Architecture principles, dividing logic into layers:
    - `CosmoChess.Domain`: Core entities, interfaces, and domain logic.
    - `CosmoChess.Application`: Application logic, CQRS commands, and handlers.
    - `CosmoChess.Infrastructure`: Implementation of interfaces, database access (Entity Framework Core), authentication, and external services (Stockfish).
    - `CosmoChess.Api`: The API layer based on ASP.NET Core, which provides RESTful endpoints and manages real-time communication via SignalR.
- **Database:** PostgreSQL with automatic migrations on startup.
- **Authentication:** JWT (JSON Web Tokens) based.
- **Real-time:** SignalR for WebSocket-based real-time gameplay (endpoint: `/api/gamehub`).

### Web Frontend

- **Framework:** Vue.js
- **Build Tool:** Vite
- **Production Serving:** Nginx (inside frontend container on port 8080)
- **Backend Interaction:**
    - REST API for core operations (game creation, login) via `/api/` endpoint.
    - WebSockets (via SignalR client) for real-time gameplay via `/api/gamehub`.

### Mobile (Android)

- **Language:** Kotlin
- **Min SDK:** 24 (Android 7.0+)
- **Architecture:** MVVM with Repository pattern
- **Networking:** Retrofit (REST), SignalR (WebSocket)
- **Features:**
    - Native Android app with Material Design
    - Play vs Bot with all difficulty levels and styles
    - Online multiplayer with real-time synchronization
    - Captured pieces display and move history
    - Legal move validation and highlighting
    - Beautiful Unicode chess pieces (♔♕♖♗♘♙)

### Infrastructure

- **Containerization:** Docker and Docker Compose for creating and managing the environment.
- **Web Server/Proxy:** Nginx is used as a reverse proxy to route requests:
    - `/` → Frontend container (nginx on port 8080)
    - `/api/` → Backend container (ASP.NET on port 5000)
    - `/api/gamehub` → SignalR WebSocket hub
- **CI/CD:** GitHub Actions for automated builds and deployment.

## Getting Started

### Local Development

**Prerequisites:**
- Docker
- Docker Compose

**Steps to run:**

1.  **Clone the repository:**
    ```bash
    git clone <repository_url>
    cd CosmoChess
    ```

2.  **Run the services using Docker Compose:**
    ```bash
    docker-compose up --build
    ```
    This command will build the images for the backend and frontend, run all necessary containers (database, backend, frontend, nginx), and link them together.

3.  **Open the application in your browser:**
    After all containers have started successfully, the application will be available at:
    [http://localhost](http://localhost)

### Production Deployment

The project uses GitHub Actions for automated deployment to production.

**Workflow:**
1. Push to `main` branch triggers Docker image builds
2. Images are pushed to GitHub Container Registry (GHCR)
3. Deployment workflow pulls latest images and restarts containers on the server

**Production configuration:**
- Uses `docker-compose.prod.yml` with pre-built images from GHCR
- Environment variables configured for production
- All services run in isolated Docker network

## Project Structure

- `backend/`: Source code for the .NET backend application. [See README](backend/README.md)
- `frontend/`: Source code for the Vue.js web frontend application. [See README](frontend/README.md)
- `android-app/`: Native Android mobile application. [See README](android-app/README.md)
- `database/`: Contains scripts for database initialization.
- `nginx/`: Configuration files for Nginx reverse proxy. [See README](nginx/README.md)
- `docker-compose.yml`: Development environment configuration.
- `docker-compose.prod.yml`: Production environment configuration.
- `.github/workflows/`: CI/CD pipelines for build and deployment.

## Documentation

- [Configuration Reference](CONFIGURATION.md) - Important configuration details and common mistakes
- [Deployment Guide](DEPLOYMENT.md) - Production deployment instructions
- [Backend Documentation](backend/README.md) - Backend architecture and API
- [Web Frontend Documentation](frontend/README.md) - Frontend setup and components
- [Android App Documentation](android-app/README.md) - Mobile app setup and features
- [Nginx Configuration](nginx/README.md) - Reverse proxy setup