FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

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

RUN mkdir -p /bundle \
 && dotnet ef migrations bundle \
    --project src/FinBeat.TaskManagement.Infrastructure \
    --startup-project src/FinBeat.TaskManagement.Infrastructure \
    --context ApplicationDbContext \
    --configuration Release --force --output /bundle/migrate

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

COPY --from=build /bundle/migrate ./migrate

RUN chmod +x ./migrate

USER $APP_UID

ENTRYPOINT ["./migrate"]
