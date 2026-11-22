# syntax=docker/dockerfile:1

# Build context: the 'src' directory (see src/.dockerignore).
#   docker build -f Fydar.Dev.WebApp/Dockerfile -t fydar-dev .

# .NET 11 images are published for Ubuntu 26.04 ("resolute").
ARG DOTNET_VERSION=11.0
ARG DISTRO=resolute

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-${DISTRO} AS base

# HTTP/3 needs libmsquic, which only Microsoft's package feed provides.
ARG DEBIAN_FRONTEND=noninteractive
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl \
    && . /etc/os-release \
    && curl -fsSL "https://packages.microsoft.com/config/ubuntu/${VERSION_ID}/packages-microsoft-prod.deb" -o /tmp/packages-microsoft-prod.deb \
    && dpkg -i /tmp/packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y --no-install-recommends libmsquic \
    && rm -rf /var/lib/apt/lists/* /tmp/packages-microsoft-prod.deb

WORKDIR /app

# The endpoints themselves are declared by 'Kestrel:Endpoints' in appsettings.Production.json;
# clearing the base image's default keeps it from announcing a port nothing binds.
ENV ASPNETCORE_HTTP_PORTS=

EXPOSE 80
EXPOSE 443/tcp
EXPOSE 443/udp

# Run as the image's built-in non-root user. The numeric ID lets orchestrators verify
# 'runAsNonRoot' without resolving names.
USER $APP_UID

# /api/health is mapped before HTTPS redirection, so plain HTTP on the loopback is enough.
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD ["curl", "-fsS", "-o", "/dev/null", "http://localhost/api/health"]


# The build is architecture-neutral (framework-dependent, no apphost), so it runs on the
# native build platform even when the target platform differs.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-${DISTRO} AS build
ARG BUILD_CONFIGURATION=Release
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
WORKDIR /src

# Restore from the project files alone, so source edits don't invalidate the restore layer.
COPY --link --parents **/*.csproj ./
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore "Fydar.Dev.WebApp/Fydar.Dev.WebApp.csproj"

COPY --link . .
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish "Fydar.Dev.WebApp/Fydar.Dev.WebApp.csproj" \
        --no-restore \
        -c "$BUILD_CONFIGURATION" \
        -o /app/publish \
        -p:UseAppHost=false


FROM base AS final
LABEL org.opencontainers.image.title="fydar.dev" \
      org.opencontainers.image.source="https://github.com/Fydar/fydar.dev"

WORKDIR /app

# The large, rarely changing game builds and media get layers of their own, so an ordinary
# code change only re-ships the small application layer.
COPY --link --from=build /app/publish/wwwroot/play ./wwwroot/play
COPY --link --from=build /app/publish/wwwroot/media ./wwwroot/media
COPY --link --from=build --exclude=wwwroot/play --exclude=wwwroot/media /app/publish .

ENTRYPOINT ["dotnet", "Fydar.Dev.WebApp.dll"]
