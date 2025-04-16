FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LDTTeam.Authentication.Server/LDTTeam.Authentication.Server.csproj", "LDTTeam.Authentication.Server/"]
RUN dotnet restore "LDTTeam.Authentication.Server/LDTTeam.Authentication.Server.csproj"
COPY . .
WORKDIR "/src/LDTTeam.Authentication.Server"
RUN dotnet build "LDTTeam.Authentication.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LDTTeam.Authentication.Server.csproj" -c Release -o /app/publish 

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LDTTeam.Authentication.Server.dll"]
