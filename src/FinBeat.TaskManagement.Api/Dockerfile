FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

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

RUN mkdir -p /app/logs && chown $APP_UID:$APP_UID /app/logs

COPY --from=build /app .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "FinBeat.TaskManagement.Api.dll"]
