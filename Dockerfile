# syntax=docker/dockerfile:1

# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies first for better layer caching
COPY PriorityHub.sln ./
COPY backend/PriorityHub.Api/PriorityHub.Api.csproj            backend/PriorityHub.Api/
COPY backend/PriorityHub.Api.Tests/PriorityHub.Api.Tests.csproj backend/PriorityHub.Api.Tests/
COPY backend/PriorityHub.Ui/PriorityHub.Ui.csproj              backend/PriorityHub.Ui/
COPY backend/PriorityHub.Ui.Tests/PriorityHub.Ui.Tests.csproj  backend/PriorityHub.Ui.Tests/
COPY backend/Directory.Build.props                             backend/

RUN dotnet restore backend/PriorityHub.Ui/PriorityHub.Ui.csproj

# Copy remaining source and publish
COPY backend/ backend/
RUN dotnet publish backend/PriorityHub.Ui/PriorityHub.Ui.csproj \
        --no-restore \
        --configuration Release \
        --output /app/publish

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PriorityHub.Ui.dll"]
