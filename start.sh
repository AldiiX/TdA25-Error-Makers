#!/bin/bash

# picovinky
echo "fs.inotify.max_user_instances=512" >> /etc/sysctl.conf && \
echo "fs.inotify.max_user_watches=524288" >> /etc/sysctl.conf && \
sysctl -p &

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
mysql -e "ALTER USER 'tda25'@'%' IDENTIFIED WITH mysql_native_password BY 'password';"
mysql -e "GRANT ALL PRIVILEGES ON tda25.* TO 'admin'@'%';"
mysql -e "GRANT ALL PRIVILEGES ON tda25.* TO 'tda25'@'%';"
mysql -e "FLUSH PRIVILEGES;"

# Start the application
/app/build/TdA25-Error-Makers &

# Keep the container running
tail -f /dev/null