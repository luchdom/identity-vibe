SELECT 'CREATE DATABASE "Orders"'
WHERE NOT EXISTS (
    SELECT FROM pg_database WHERE datname = 'Orders'
)\gexec
