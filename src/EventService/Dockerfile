FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/BuildingBlocks/Contracts/ src/BuildingBlocks/Contracts/
COPY src/EventService/ src/EventService/
RUN dotnet publish src/EventService/EventService.Api/EventService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "EventService.Api.dll"]
