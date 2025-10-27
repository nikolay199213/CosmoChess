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
- **Database:** PostgreSQL.
- **Authentication:** JWT (JSON Web Tokens) based.
- **Real-time:** SignalR for real-time data exchange.

### Frontend

- **Framework:** Vue.js
- **Build Tool:** Vite
- **Backend Interaction:**
    - REST API for core operations (game creation, login).
    - WebSockets (via SignalR client) for real-time gameplay.

### Infrastructure

- **Containerization:** Docker and Docker Compose for creating and managing the environment.
- **Web Server/Proxy:** Nginx is used as a reverse proxy to route requests to the frontend and backend services.

## Getting Started

The project is fully containerized, so only Docker is required to run it.

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

## Project Structure

- `backend/`: Source code for the .NET backend application.
- `frontend/`: Source code for the Vue.js frontend application.
- `database/`: Contains scripts for database initialization.
- `nginx/`: Configuration files for Nginx.
- `docker-compose.yml`: The main file for orchestrating all project services.