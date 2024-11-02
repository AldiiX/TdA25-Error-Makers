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

# Spuštění všeho
CMD /app/start.sh