FROM ubuntu:22.04 AS base

# Instalace závislostí: Redis, MySQL, wget, dos2unix a .NET SDK
RUN apt-get update && apt-get install -y \
    redis \
    mysql-server \
    wget \
    dos2unix \
    apt-transport-https \
    ca-certificates \
    && wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update && apt-get install -y dotnet-sdk-8.0 \
    && apt-get clean

# Nastavení environment proměnných
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false

# Exponování portu
EXPOSE 80

# Build projektu
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TdA25-Error-Makers.csproj", "./"]
RUN dotnet restore "TdA25-Error-Makers.csproj"
COPY . .

# Vytvoření .env souboru v adresáři /src, pokud ještě neexistuje
RUN if [ ! -f /src/.env ]; then \
    echo "DATABASE_PASSWORD=password" >> /src/.env && \
    echo "CACHE_VERSION=1" >> /src/.env && \
    echo "DATABASE_IP=localhost" >> /src/.env; \
    fi

RUN dotnet build "TdA25-Error-Makers.csproj" -c $BUILD_CONFIGURATION -o /app/build --runtime linux-x64

# Kopírování databázového skriptu a startovacího souboru
COPY database.sql /app/
COPY start.sh /app/

# Ujisti se, že start.sh má správnou shebang (např. #!/bin/sh nebo #!/bin/bash)
# a převedeme jej na unixový formát, aby nedocházelo k chybě exec formátu
RUN dos2unix /app/start.sh

# Nastavení spustitelných práv
RUN chmod +x /app/start.sh

# Publish aplikace
RUN dotnet publish "TdA25-Error-Makers.csproj" -c $BUILD_CONFIGURATION -o /app/ --runtime linux-x64 /p:UseAppHost=false

# Přepnutí do adresáře /app/
WORKDIR /app/

# Instalace tzdata a konfigurace časového pásma
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

# Vytvoření .env souboru v adresáři /app
RUN echo "DATABASE_PASSWORD=${DATABASE_PASSWORD}" >> /app/.env && \
    echo "CACHE_VERSION=${CACHE_VERSION}" >> /app/.env && \
    echo "DATABASE_IP=${DATABASE_IP}" >> /app/.env

# Spuštění startovacího skriptu při startu kontejneru
CMD ["/app/start.sh"]