# Use the official .NET 9 runtime image as the base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Install curl for health checks and netcat for database connectivity check
RUN apt-get update && apt-get install -y curl netcat-traditional && rm -rf /var/lib/apt/lists/*

# Use the official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["ProcurementAPI.csproj", "./"]
RUN dotnet restore "ProcurementAPI.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build "ProcurementAPI.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "ProcurementAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app

# Copy the published application from the build stage
COPY --from=publish /app/publish .

# Copy the wait script
COPY wait-for-db.sh /app/wait-for-db.sh
RUN chmod +x /app/wait-for-db.sh

# Install debugging tools and shell
RUN apt-get update && apt-get install -y \
    procps \
    curl \
    netcat-traditional \
    bash \
    && rm -rf /var/lib/apt/lists/*

# Create a non-root user for security (but don't switch to it for debugging)
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app

# Set environment to Development for Swagger
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:8080

# Set the entry point
ENTRYPOINT ["/app/wait-for-db.sh"] 
