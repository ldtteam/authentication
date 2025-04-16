ARG PROJECT_VERSION=0.5.0
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LDTTeam.Authentication.Server/LDTTeam.Authentication.Server.csproj", "LDTTeam.Authentication.Server/"]
RUN dotnet restore "LDTTeam.Authentication.Server/LDTTeam.Authentication.Server.csproj" -p:Version=${PROJECT_VERSION} -p:AssemblyVersion=${PROJECT_VERSION}
COPY . .
WORKDIR "/src/LDTTeam.Authentication.Server"
RUN dotnet build "LDTTeam.Authentication.Server.csproj" -c Release -o /app/build -p:Version=${PROJECT_VERSION} -p:AssemblyVersion=${PROJECT_VERSION}

FROM build AS publish
RUN dotnet publish "LDTTeam.Authentication.Server.csproj" -c Release -o /app/publish -p:Version=${PROJECT_VERSION} -p:AssemblyVersion=${PROJECT_VERSION}

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LDTTeam.Authentication.Server.dll"]
