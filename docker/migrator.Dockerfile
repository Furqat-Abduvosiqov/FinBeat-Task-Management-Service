# Applies migrations as a one-shot container, so schema changes stay a deploy step. A host that
# migrates on boot races every other instance and needs DDL rights at runtime; this does neither.
#
# Build context is the repository root:
#   docker build -f docker/migrator.Dockerfile .
#
# It carries the SDK because dotnet-ef needs it. That is a large image for one command, and the
# reason it is a separate service that exits rather than anything the running hosts contain.

FROM mcr.microsoft.com/dotnet/sdk:8.0
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

COPY src/ src/

# Built here rather than on every run, so the container starts applying migrations instead of
# compiling. The configuration has to match the one the entrypoint passes, or it builds again.
RUN dotnet build src/FinBeat.TaskManagement.Infrastructure/FinBeat.TaskManagement.Infrastructure.csproj \
    --configuration Release --no-restore

# ConnectionStrings__TaskManagement is read by DesignTimeApplicationDbContextFactory.
ENTRYPOINT ["dotnet", "ef", "database", "update", \
    "--project", "src/FinBeat.TaskManagement.Infrastructure", \
    "--startup-project", "src/FinBeat.TaskManagement.Infrastructure", \
    "--context", "ApplicationDbContext", \
    "--configuration", "Release", "--no-build"]
