# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and all project files
COPY ["CosmoChess.sln", "."]
COPY ["CosmoChess.Api/CosmoChess.Api.csproj", "CosmoChess.Api/"]
COPY ["CosmoChess.Application/CosmoChess.Application.csproj", "CosmoChess.Application/"]
COPY ["CosmoChess.Domain/CosmoChess.Domain.csproj", "CosmoChess.Domain/"]
COPY ["CosmoChess.Infrastructure/CosmoChess.Infrastructure.csproj", "CosmoChess.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "CosmoChess.Api/CosmoChess.Api.csproj"

# Copy all source code
COPY . .

# Build
WORKDIR "/src/CosmoChess.Api"
RUN dotnet build "CosmoChess.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "CosmoChess.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "CosmoChess.Api.dll"]