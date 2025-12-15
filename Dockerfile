# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ChronicleHub.sln ./
COPY src/ChronicleHub.Api/ChronicleHub.Api.csproj src/ChronicleHub.Api/
COPY src/ChronicleHub.Application/ChronicleHub.Application.csproj src/ChronicleHub.Application/
COPY src/ChronicleHub.Domain/ChronicleHub.Domain.csproj src/ChronicleHub.Domain/
COPY src/ChronicleHub.Infrastructure/ChronicleHub.Infrastructure.csproj src/ChronicleHub.Infrastructure/
COPY tests/ tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ src/

# Build and publish
WORKDIR /src/src/ChronicleHub.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published app
COPY --from=build /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "ChronicleHub.Api.dll"]
