# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY CosmoChess.EngineService.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o /app/out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install Stockfish
RUN apt-get update && \
    apt-get install -y stockfish && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/out .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5001
ENV STOCKFISH_PATH=/usr/games/stockfish
ENV STOCKFISH_HASH_SIZE=1024
ENV STOCKFISH_THREADS=4
ENV STOCKFISH_DEFAULT_DEPTH=22
ENV STOCKFISH_TIMEOUT_SECONDS=60

EXPOSE 5001

ENTRYPOINT ["dotnet", "CosmoChess.EngineService.dll"]
