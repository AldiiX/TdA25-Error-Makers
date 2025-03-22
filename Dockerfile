# Define the user ID (default value 1000)
ARG APP_UID=1000

# ------------------------
# Base stage pro běh ASP.NET aplikace (použito pro build)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false

# Stage to install Node.js (na základě SDK)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS with-node
RUN apt-get update && apt-get install -y curl
RUN curl -sL https://deb.nodesource.com/setup_20.x | bash && apt-get install -y nodejs

# Stage to build the backend
FROM with-node AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TdA25-Error-Makers.Server/TdA25-Error-Makers.Server.csproj", "TdA25-Error-Makers.Server/"]
COPY ["tda25-error-makers.client/tda25-error-makers.client.esproj", "tda25-error-makers.client/"]
RUN dotnet restore "./TdA25-Error-Makers.Server/TdA25-Error-Makers.Server.csproj"
COPY . .
WORKDIR "/src/TdA25-Error-Makers.Server"
RUN dotnet build "./TdA25-Error-Makers.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage to publish the backend
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TdA25-Error-Makers.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ------------------------
# Final stage: založeno na Ubuntu 22.04 (jammy)
FROM ubuntu:22.04 AS final
ARG APP_UID=1000
WORKDIR /app

# Instalace základních balíčků
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    dos2unix \
    apt-transport-https \
    ca-certificates \
    gnupg \
    lsb-release \
    nginx \
    mysql-server \
    redis-server

# Instalace tzdata a konfigurace časového pásma
ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update && apt-get install -y --no-install-recommends tzdata \
    && ln -sf /usr/share/zoneinfo/Europe/Prague /etc/localtime \
    && echo "Europe/Prague" > /etc/timezone \
    && dpkg-reconfigure -f noninteractive tzdata \
    && apt-get clean

# Instalace .NET runtime (např. aspnetcore-runtime-9.0)
RUN wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y aspnetcore-runtime-9.0

# Instalace Node.js
RUN curl -sL https://deb.nodesource.com/setup_20.x | bash && apt-get install -y nodejs

# Nastavení proměnných pro .env (build args se předají)
ARG DATABASE_IP=localhost
ARG DATABASE_PASSWORD=password
ARG CACHE_VERSION=1
RUN echo "DATABASE_PASSWORD=${DATABASE_PASSWORD}" >> /app/.env && \
    echo "CACHE_VERSION=${CACHE_VERSION}" >> /app/.env && \
    echo "DATABASE_IP=${DATABASE_IP}" >> /app/.env

# Kopírování publikovaných souborů backendu
COPY --from=publish /app/publish .

# Kopírování souboru database.sql
COPY database.sql /app/database.sql

# Kopírování frontendových souborů a oprava práv
COPY ["tda25-error-makers.client/", "/app/client/"]
RUN chown -R ${APP_UID}:${APP_UID} /app/client
WORKDIR /app/client

# Nastavení cache a instalace závislostí pro frontend
RUN npm config set cache /app/.npm
RUN npm install --unsafe-perm

# Build frontend
RUN npm run build

# Kopírování konfigurace Nginx
COPY nginx.conf /etc/nginx/nginx.conf

# Příprava startovacího skriptu
WORKDIR /app
COPY --chmod=0755 start.sh .
CMD ["./start.sh"]