# Build context is the repository root, not this folder: the build needs global.json and the
# Directory.*.props that pin the SDK and every package version.
#   docker build -f src/FinBeat.TaskManagement.Api/Dockerfile .
#
# The SDK/runtime/aspnet base images below stay on floating :8.0 tags rather than digests, unlike
# the pulled images in docker-compose.yml. Microsoft repoints them roughly monthly with
# security-only patches, and this repo has no CI to refresh a pinned digest - pinning here would
# silently stop those patches from reaching the image until a human remembered to re-pin, which is
# a worse default than the drift postgres/rabbitmq/jaeger are pinned against. Every Dockerfile that
# references a given tag has to stay on the same tag either way, or ContainerImageTests fails.

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

COPY src/FinBeat.TaskManagement.Domain/ src/FinBeat.TaskManagement.Domain/
COPY src/FinBeat.TaskManagement.Contracts/ src/FinBeat.TaskManagement.Contracts/
COPY src/FinBeat.TaskManagement.Application/ src/FinBeat.TaskManagement.Application/
COPY src/FinBeat.TaskManagement.Infrastructure/ src/FinBeat.TaskManagement.Infrastructure/
COPY src/FinBeat.TaskManagement.Api/ src/FinBeat.TaskManagement.Api/
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
