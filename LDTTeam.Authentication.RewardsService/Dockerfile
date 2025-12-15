FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["LDTTeam.Authentication.RewardsService/LDTTeam.Authentication.RewardsService.csproj", "LDTTeam.Authentication.RewardsService/"]
RUN dotnet restore "LDTTeam.Authentication.RewardsService/LDTTeam.Authentication.RewardsService.csproj"
COPY . .
WORKDIR "/src/LDTTeam.Authentication.RewardsService"
RUN dotnet build "./LDTTeam.Authentication.RewardsService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LDTTeam.Authentication.RewardsService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LDTTeam.Authentication.RewardsService.dll"]
