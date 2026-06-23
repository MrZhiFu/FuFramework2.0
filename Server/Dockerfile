FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["FuFramework.Launcher/FuFramework.Launcher.csproj", "FuFramework.Launcher/"]
COPY ["FuFramework.Config/FuFramework.Config.csproj", "FuFramework.Config/"]
COPY ["FuFramework.Apps/FuFramework.Apps.csproj", "FuFramework.Apps/"]
COPY ["FuFramework.Proto/FuFramework.Proto.csproj", "FuFramework.Proto/"]

RUN dotnet restore "FuFramework.Launcher/FuFramework.Launcher.csproj"
COPY . .
WORKDIR "/src/FuFramework.Launcher"
RUN dotnet build "FuFramework.Launcher.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FuFramework.Launcher.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FuFramework.Launcher.dll"]
