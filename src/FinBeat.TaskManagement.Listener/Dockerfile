FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/FinBeat.TaskManagement.Contracts/*.csproj src/FinBeat.TaskManagement.Contracts/
COPY src/FinBeat.TaskManagement.Listener/*.csproj src/FinBeat.TaskManagement.Listener/
RUN dotnet restore src/FinBeat.TaskManagement.Listener/FinBeat.TaskManagement.Listener.csproj

COPY src/FinBeat.TaskManagement.Contracts/ src/FinBeat.TaskManagement.Contracts/
COPY src/FinBeat.TaskManagement.Listener/ src/FinBeat.TaskManagement.Listener/
RUN dotnet publish src/FinBeat.TaskManagement.Listener/FinBeat.TaskManagement.Listener.csproj \
    --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

RUN mkdir -p /app/logs && chown $APP_UID:$APP_UID /app/logs

COPY --from=build /app .

USER $APP_UID
ENTRYPOINT ["dotnet", "FinBeat.TaskManagement.Listener.dll"]
