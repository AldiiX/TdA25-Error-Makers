#!/usr/bin/bash

redis-server &
/app/build/TdA25-Error-Makers &

mysqld_safe && mysql tda25 < /app/database.sql