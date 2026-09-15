# Applies migrations as a one-shot container, so schema changes stay a deploy step. A host that
# migrates on boot races every other instance and needs DDL rights at runtime; this does neither.
#
# Build context is the repository root:
#   docker build -f docker/migrator.Dockerfile .
#
# The final image is the runtime, not the SDK: `dotnet ef migrations bundle` compiles the migrations
# into a single-file, framework-dependent executable in the build stage, so the final image needs
# the .NET runtime but no SDK, no dotnet-ef, no MSBuild and no NuGet.
#
# Floating :8.0 tags, not digests - see src/FinBeat.TaskManagement.Api/Dockerfile for the
# reasoning, and keep this in step with the Api's and Listener's sdk:8.0 / runtime:8.0 tags, or
# ContainerImageTests fails.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# The version of dotnet-ef is pinned by the repository's tool manifest, not by whatever is newest.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY .config/ .config/
RUN dotnet tool restore

COPY src/FinBeat.TaskManagement.Domain/*.csproj src/FinBeat.TaskManagement.Domain/
COPY src/FinBeat.TaskManagement.Contracts/*.csproj src/FinBeat.TaskManagement.Contracts/
COPY src/FinBeat.TaskManagement.Application/*.csproj src/FinBeat.TaskManagement.Application/
COPY src/FinBeat.TaskManagement.Infrastructure/*.csproj src/FinBeat.TaskManagement.Infrastructure/
RUN dotnet restore src/FinBeat.TaskManagement.Infrastructure/FinBeat.TaskManagement.Infrastructure.csproj

COPY src/FinBeat.TaskManagement.Domain/ src/FinBeat.TaskManagement.Domain/
COPY src/FinBeat.TaskManagement.Contracts/ src/FinBeat.TaskManagement.Contracts/
COPY src/FinBeat.TaskManagement.Application/ src/FinBeat.TaskManagement.Application/
COPY src/FinBeat.TaskManagement.Infrastructure/ src/FinBeat.TaskManagement.Infrastructure/

# One build, one configuration flag: the bundle compiles the project itself, so there is no second
# --configuration for it to disagree with. No --target-runtime either - this runs inside the Linux
# SDK image, so the default RID is already that of the runtime stage below, on x64 and arm64 alike.
RUN mkdir -p /bundle \
 && dotnet ef migrations bundle \
    --project src/FinBeat.TaskManagement.Infrastructure \
    --startup-project src/FinBeat.TaskManagement.Infrastructure \
    --context ApplicationDbContext \
    --configuration Release --force --output /bundle/migrate

# A bundle is an ordinary executable: no SDK, no dotnet-ef, nothing to keep in sync.
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

COPY --from=build /bundle/migrate ./migrate

# COPY preserves the executable bit from the build stage; this only guards against a platform that
# does not.
RUN chmod +x ./migrate

USER $APP_UID

# ConnectionStrings__TaskManagement is read by DesignTimeApplicationDbContextFactory, which the
# bundle carries - so no --connection argument is needed and compose passes it as the environment.
ENTRYPOINT ["./migrate"]
