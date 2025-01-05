FROM ubuntu:22.04 AS base

# instalace Redis, MySQL, wget a .NET SDK
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

# env
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false

# exposování portů
EXPOSE 80

# buildnutí projektu
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TdA25-Error-Makers.csproj", "./"]
RUN dotnet restore "TdA25-Error-Makers.csproj"
COPY . .
RUN dotnet build "TdA25-Error-Makers.csproj" -c $BUILD_CONFIGURATION -o /app/build --runtime linux-x64
COPY database.sql /app/
COPY start.sh /app/
RUN chmod +x /app/start.sh

# pushnutí aplikace do /app/
RUN dotnet publish "TdA25-Error-Makers.csproj" -c $BUILD_CONFIGURATION -o /app/ --runtime linux-x64 /p:UseAppHost=false

# přepnutí do /app/ diru
WORKDIR /app/

# instalace balíčku tzdata a konfigurace časové zóny
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
#ARG REDIS_PASSWORD=default
#ARG REDIS_PORT=6379
ARG CACHE_VERSION=1

# vytvoření .env souboru ve složce /app
RUN echo "DATABASE_PASSWORD=${DATABASE_PASSWORD}" >> /app/.env && \
    #echo "REDIS_PASSWORD=${REDIS_PASSWORD}" >> /app/.env && \
    #echo "REDIS_PORT=${REDIS_PORT}" >> /app/.env && \
    echo "CACHE_VERSION=${CACHE_VERSION}" >> /app/.env && \
    echo "DATABASE_IP=${DATABASE_IP}" >> /app/.env

# použití systémových limitů v runtime kontejneru
CMD /app/start.sh