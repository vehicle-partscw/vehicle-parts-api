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

# quest pdf needs fontconfig + a real font set on the container, otherwise
# pdf generation hangs forever when it tries to render any text. dejavu
# covers latin + extended chars without bloating the image too much.
RUN apt-get update \
    && apt-get install -y --no-install-recommends fontconfig fonts-dejavu libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Render injects PORT (default 10000); ASP.NET listens on whatever URLs we tell it
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# disable hot-reload of appsettings.json - we don't change config at runtime in prod
# and the inotify watchers blow past render's per-container fd limit on cold start.
ENV DOTNET_hostBuilder__reloadConfigOnChange=false
ENV DOTNET_USE_POLLING_FILE_WATCHER=1

EXPOSE 8080
ENTRYPOINT ["dotnet", "AutoParts.WebApi.dll"]
