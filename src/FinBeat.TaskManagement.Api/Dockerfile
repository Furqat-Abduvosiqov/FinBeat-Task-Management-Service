# Build context is the repository root, not this folder: the build needs global.json and the
# Directory.*.props that pin the SDK and every package version.
#   docker build -f src/FinBeat.TaskManagement.Api/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Manifests first, so a source-only edit reuses the restore layer instead of re-downloading NuGet.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/FinBeat.TaskManagement.Domain/*.csproj src/FinBeat.TaskManagement.Domain/
COPY src/FinBeat.TaskManagement.Contracts/*.csproj src/FinBeat.TaskManagement.Contracts/
COPY src/FinBeat.TaskManagement.Application/*.csproj src/FinBeat.TaskManagement.Application/
COPY src/FinBeat.TaskManagement.Infrastructure/*.csproj src/FinBeat.TaskManagement.Infrastructure/
COPY src/FinBeat.TaskManagement.Api/*.csproj src/FinBeat.TaskManagement.Api/
RUN dotnet restore src/FinBeat.TaskManagement.Api/FinBeat.TaskManagement.Api.csproj

COPY src/ src/
RUN dotnet publish src/FinBeat.TaskManagement.Api/FinBeat.TaskManagement.Api.csproj \
    --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Serilog rolls a file under logs/, and this image runs as a non-root user that cannot create it.
RUN mkdir -p /app/logs && chown $APP_UID:$APP_UID /app/logs

COPY --from=build /app .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "FinBeat.TaskManagement.Api.dll"]
