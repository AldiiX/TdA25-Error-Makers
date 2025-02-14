FROM ubuntu:22.04 AS base

# Install Redis, MySQL, wget, and .NET SDK
RUN apt-get update && apt-get install -y \
    redis \
    mysql-server \
    wget \
    apt-transport-https \
    ca-certificates \
    && wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update && apt-get install -y dotnet-sdk-8.0 \
    && apt-get clean

# Environment variables
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false

# Expose ports
EXPOSE 80

# Build project
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TdA25-Error-Makers.csproj", "./"]
RUN dotnet restore "TdA25-Error-Makers.csproj"
COPY . .

# Create .env file if it does not exist
RUN if [ ! -f /src/.env ]; then \
    echo "DATABASE_PASSWORD=password" >> /src/.env && \
    echo "CACHE_VERSION=1" >> /src/.env && \
    echo "DATABASE_IP=localhost" >> /src/.env; \
    fi

RUN dotnet build "TdA25-Error-Makers.csproj" -c $BUILD_CONFIGURATION -o /app/build --runtime linux-x64
COPY database.sql /app/
COPY start.sh /app/
RUN chmod +x /app/start.sh

# Publish application
RUN dotnet publish "TdA25-Error-Makers.csproj" -c $BUILD_CONFIGURATION -o /app/ --runtime linux-x64 /p:UseAppHost=false

# Switch to /app directory
WORKDIR /app/

# Install tzdata package and configure timezone
ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update && apt-get install -y --no-install-recommends tzdata \
    && ln -sf /usr/share/zoneinfo/Europe/Prague /etc/localtime \
    && echo "Europe/Prague" > /etc/timezone \
    && dpkg-reconfigure -f noninteractive tzdata \
    && apt-get clean

# build args aby fungovaly .env věci
# ----- pokud to chce někdo měnit tak zkontrolujte že tohle a váš .env soubor má stejné názvy proměnných
#       pak se to musí změnit v souboru .github/workflows/pipeline.yml
#       a pak v github actions secrets -> https://github.com/AldiiX/TdA25-Error-Makers/settings/secrets/actions
ARG DATABASE_IP=localhost
ARG DATABASE_PASSWORD=password
ARG CACHE_VERSION=1

# Create .env file in /app directory
RUN echo "DATABASE_PASSWORD=${DATABASE_PASSWORD}" >> /app/.env && \
    echo "CACHE_VERSION=${CACHE_VERSION}" >> /app/.env && \
    echo "DATABASE_IP=${DATABASE_IP}" >> /app/.env

# Use system limits in runtime container
CMD ["/app/start.sh"]