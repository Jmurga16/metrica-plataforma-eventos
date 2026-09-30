FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY ["src/BuildingBlocks/Contracts/BuildingBlocks.Contracts.csproj", "src/BuildingBlocks/Contracts/"]
COPY ["src/NotificationService/NotificationService.Domain/NotificationService.Domain.csproj", "src/NotificationService/NotificationService.Domain/"]
COPY ["src/NotificationService/NotificationService.Application/NotificationService.Application.csproj", "src/NotificationService/NotificationService.Application/"]
COPY ["src/NotificationService/NotificationService.Infrastructure/NotificationService.Infrastructure.csproj", "src/NotificationService/NotificationService.Infrastructure/"]
COPY ["src/NotificationService/NotificationService.Api/NotificationService.Api.csproj", "src/NotificationService/NotificationService.Api/"]
RUN dotnet restore "src/NotificationService/NotificationService.Api/NotificationService.Api.csproj"
COPY . .
RUN find src/BuildingBlocks/Contracts src/NotificationService \
    -type d \( -name bin -o -name obj \) -prune -exec rm -rf '{}' +
RUN dotnet publish "src/NotificationService/NotificationService.Api/NotificationService.Api.csproj" \
    --configuration Release --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
RUN apk add --no-cache curl icu-libs krb5-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
USER app
COPY --from=build --chown=app:app /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "NotificationService.Api.dll"]
