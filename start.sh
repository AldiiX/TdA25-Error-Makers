#!/bin/sh



# Start Redis server
redis-server &

# Start MySQL and initialize the database with user accounts
mysqld_safe &

# Wait for MySQL server to be ready
echo "Waiting for MySQL to start..."
until mysqladmin ping -h "localhost" --silent; do
    sleep 1
done
echo "MySQL is up and running."

# Initialize the database and create users
mysql -e "CREATE DATABASE IF NOT EXISTS tda25;"
mysql -e "USE tda25; SOURCE /app/database.sql;"
mysql -e "CREATE USER IF NOT EXISTS 'admin'@'%' IDENTIFIED BY 'password';"
mysql -e "CREATE USER IF NOT EXISTS 'tda25'@'%' IDENTIFIED BY 'password';"
mysql -e "ALTER USER 'tda25'@'%' IDENTIFIED BY 'password';"
mysql -e "GRANT ALL PRIVILEGES ON tda25.* TO 'admin'@'%';"
mysql -e "GRANT ALL PRIVILEGES ON tda25.* TO 'tda25'@'%';"
mysql -e "FLUSH PRIVILEGES;"


# Start the backend in the background
dotnet TdA25-Error-Makers.Server.dll &

# Start the frontend (vite preview) in the background
cd /app/client
npm run preview -- --port 8115 --host 0.0.0.0 &

# Start Nginx
nginx -g 'daemon off;'