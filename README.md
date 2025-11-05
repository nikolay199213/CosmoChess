# CosmoChess

CosmoChess is a full-stack web application for playing chess in real-time. It includes user authentication, creating and joining games, and position analysis using a chess engine.

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

### Frontend

- **Framework:** Vue.js
- **Build Tool:** Vite
- **Production Serving:** Nginx (inside frontend container on port 8080)
- **Backend Interaction:**
    - REST API for core operations (game creation, login) via `/api/` endpoint.
    - WebSockets (via SignalR client) for real-time gameplay via `/api/gamehub`.

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
- `frontend/`: Source code for the Vue.js frontend application. [See README](frontend/README.md)
- `database/`: Contains scripts for database initialization.
- `nginx/`: Configuration files for Nginx reverse proxy. [See README](nginx/README.md)
- `docker-compose.yml`: Development environment configuration.
- `docker-compose.prod.yml`: Production environment configuration.
- `.github/workflows/`: CI/CD pipelines for build and deployment.

## Documentation

- [Configuration Reference](CONFIGURATION.md) - Important configuration details and common mistakes
- [Deployment Guide](DEPLOYMENT.md) - Production deployment instructions
- [Backend Documentation](backend/README.md) - Backend architecture and API
- [Frontend Documentation](frontend/README.md) - Frontend setup and components
- [Nginx Configuration](nginx/README.md) - Reverse proxy setup