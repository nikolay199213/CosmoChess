# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY CosmoChess.BotService.csproj .
RUN dotnet restore

# Copy source code and build
COPY . .
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .

# Environment variables (defaults)
ENV KAFKA_BOOTSTRAP_SERVERS=kafka:29092
ENV KAFKA_GROUP_ID=bot-workers
ENV KAFKA_REQUEST_TOPIC=bot-move-requests
ENV KAFKA_RESULT_TOPIC=bot-move-results
ENV ENGINE_SERVICE_URL=http://engine-service:5001

ENTRYPOINT ["dotnet", "CosmoChess.BotService.dll"]
