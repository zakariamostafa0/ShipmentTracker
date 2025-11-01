# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first (better layer caching)
COPY ShipmentTracker.sln ./
COPY ShipmentTracker.API/ShipmentTracker.API.csproj ShipmentTracker.API/
COPY ShipmentTracker.Core/ShipmentTracker.Core.csproj ShipmentTracker.Core/
COPY ShipmentTracker.Infrastructure/ShipmentTracker.Infrastructure.csproj ShipmentTracker.Infrastructure/

RUN dotnet restore "ShipmentTracker.sln"

# Copy the rest of the source
COPY . .

# Build and publish
RUN dotnet build "ShipmentTracker.sln" -c Release -o /app/build
RUN dotnet publish "ShipmentTracker.API/ShipmentTracker.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Render provides PORT; default to 8080 locally
ENV PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENV DOTNET_ENVIRONMENT=Production

# Copy published output
COPY --from=build /app/publish .

# Expose for local use; Render ignores EXPOSE
EXPOSE 8080

ENTRYPOINT ["dotnet", "ShipmentTracker.API.dll"]



