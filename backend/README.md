# CosmoChess Backend

This is the backend for the CosmoChess application, built with .NET, ASP.NET Core, and Entity Framework Core. It provides a RESTful API for game management and user authentication, and uses SignalR for real-time gameplay communication.

## Architecture

The backend follows the principles of **Clean Architecture** to create a decoupled, testable, and maintainable system. The solution is divided into four main projects:

-   `CosmoChess.Domain`: Contains the core business logic, entities (User, Game), and interfaces for repositories and services. This layer has no external dependencies.
-   `CosmoChess.Application`: Implements the application's use cases using a CQRS (Command Query Responsibility Segregation) pattern. It contains commands, queries, and their handlers. It depends only on the Domain layer.
-   `CosmoChess.Infrastructure`: Provides implementations for the interfaces defined in the Domain layer. This includes database access with Entity Framework Core, JWT generation, password hashing, and services like the Stockfish engine wrapper.
-   `CosmoChess.Api`: The entry point of the application. This ASP.NET Core project exposes the REST API endpoints and the SignalR hub. It depends on the Application and Infrastructure layers.

## Technology Stack

-   **.NET 8** (or compatible version)
-   **ASP.NET Core**: For building the REST API and SignalR hubs.
-   **Entity Framework Core**: For data access and object-relational mapping (ORM).
-   **PostgreSQL**: The database provider for EF Core.
-   **SignalR**: For real-time web functionality.
-   **MediatR**: For implementing the CQRS pattern.
-   **JWT (JSON Web Tokens)**: For securing the API.

## Local Development Setup

These instructions are for running the backend locally, without Docker.

### Prerequisites

-   [.NET SDK](https://dotnet.microsoft.com/download) (version 8 or compatible).
-   A running PostgreSQL instance.

### 1. Configure the Database Connection

1.  Open the `backend/CosmoChess.Api/appsettings.Development.json` file.
2.  Modify the `DefaultConnection` string to point to your local PostgreSQL instance. The default `docker-compose` connection string is:
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Host=localhost;Database=cosmochess;Username=postgres;Password=password123"
    }
    ```
    Adjust the `Host`, `Database`, `Username`, and `Password` to match your local setup.

### 2. Apply Database Migrations

The database schema is managed using EF Core migrations. To create and apply the migrations, you need the EF Core tools.

1.  Install the EF Core tools CLI globally if you haven't already:
    ```bash
    dotnet tool install --global dotnet-ef
    ```
2.  Navigate to the root of the backend solution (`backend/`) and run the update command:
    ```bash
    dotnet ef database update --project CosmoChess.Api --startup-project CosmoChess.Api
    ```
    *Note: You run this from the solution root, specifying the project containing migrations (`CosmoChess.Infrastructure` is the default) and the startup project (`CosmoChess.Api`).*


### 3. Run the Application

1.  Navigate to the API project directory:
    ```bash
    cd backend/CosmoChess.Api
    ```
2.  Run the project:
    ```bash
    dotnet run
    ```
The API will start, typically on `http://localhost:5000` and `https://localhost:5001`. You can see the available endpoints and test them using the Swagger UI at `/swagger`.
