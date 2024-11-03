FROM ubuntu:22.04 AS base

# Instalace Redis, MySQL, wget a .NET SDK
RUN apt-get update && apt-get install -y \
    redis \
    mysql-server \
    wget \
    apt-transport-https \
    ca-certificates \
    && wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update && apt-get install -y dotnet-sdk-8.0

# Exponování portů
EXPOSE 80

# Sestavení projektu
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TdA25-Error-Makers.csproj", "./"]
RUN dotnet restore "TdA25-Error-Makers.csproj"
COPY . .
RUN dotnet build "TdA25-Error-Makers.csproj" -c $BUILD_CONFIGURATION -o /app/build --runtime linux-x64
COPY database.sql /app/
COPY start.sh /app/
RUN chmod +x /app/start.sh

# Publikace aplikace do /app/
RUN dotnet publish "TdA25-Error-Makers.csproj" -c $BUILD_CONFIGURATION -o /app/ --runtime linux-x64 /p:UseAppHost=false

# Přepnutí do /app/ diru
WORKDIR /app/






# build args aby fungovaly .env věci
# ----- pokud to chce někdo měnit tak zkontrolujte že tohle a váš .env soubor má stejné názvy proměnných
#       pak se to musí změnit v souboru .github/workflows/pipeline.yml
#       a pak v github actions secrets -> https://github.com/AldiiX/TdA25-Error-Makers/settings/secrets/actions
ARG DATABASE_IP=localhost
ARG DATABASE_PASSWORD=default
#ARG REDIS_PASSWORD=default
#ARG REDIS_PORT=6379
ARG CACHE_VERSION=1


# Vytvoření .env souboru ve složce /app
RUN echo "DATABASE_PASSWORD=${DATABASE_PASSWORD}" >> /app/.env && \
    #echo "REDIS_PASSWORD=${REDIS_PASSWORD}" >> /app/.env && \
    #echo "REDIS_PORT=${REDIS_PORT}" >> /app/.env && \
    echo "CACHE_VERSION=${CACHE_VERSION}" >> /app/.env && \
    echo "DATABASE_IP=${DATABASE_IP}" >> /app/.env






# Spuštění všeho
CMD /app/start.sh