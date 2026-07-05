# syntax=docker/dockerfile:1.7-labs

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS base
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates \
    && curl -sSL https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -o /tmp/packages-microsoft-prod.deb \
    && dpkg -i /tmp/packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y libmsquic \
    && rm -rf /var/lib/apt/lists/* /tmp/packages-microsoft-prod.deb
USER app
WORKDIR /app

EXPOSE 80
EXPOSE 443/tcp
EXPOSE 443/udp

# Use the native build platform for faster compilation
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY --link --parents **/*.csproj ./

# Mount the NuGet cache to speed up restores
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore "./Fydar.Dev.WebApp/Fydar.Dev.WebApp.csproj"

COPY --link . .
WORKDIR "/src/Fydar.Dev.WebApp"

# Use the previously constructed NuGet cache to publish the build.
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish "./Fydar.Dev.WebApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --link --from=build /app/publish/wwwroot/play ./wwwroot/play
COPY --link --from=build /app/publish/wwwroot/media ./wwwroot/media
COPY --link --exclude=wwwroot/play/* --exclude=wwwroot/media/* --from=build /app/publish .
ENTRYPOINT ["dotnet", "Fydar.Dev.WebApp.dll"]
