# ───── Build stage ────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (cached layer if csproj files don't change)
COPY Directory.Build.props global.json ./
COPY src/AutoParts.Domain/AutoParts.Domain.csproj             src/AutoParts.Domain/
COPY src/AutoParts.Application/AutoParts.Application.csproj   src/AutoParts.Application/
COPY src/AutoParts.Infrastructure/AutoParts.Infrastructure.csproj src/AutoParts.Infrastructure/
COPY src/AutoParts.WebApi/AutoParts.WebApi.csproj             src/AutoParts.WebApi/
RUN dotnet restore src/AutoParts.WebApi/AutoParts.WebApi.csproj

# Copy the rest and publish
COPY . .
RUN dotnet publish src/AutoParts.WebApi/AutoParts.WebApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ───── Runtime stage ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render injects PORT (default 10000); ASP.NET listens on whatever URLs we tell it
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "AutoParts.WebApi.dll"]
