#!/bin/bash
set -e

# Start PostgreSQL
/etc/init.d/postgresql start

# Wait for PostgreSQL to become ready
until pg_isready -h localhost -p 5432 -U postgres
do
    echo "Waiting for PostgreSQL to start..."
    sleep 2
done

# Create a database
su - postgres -c "createdb aiden-db"

# Connect to the database and add extensions
su - postgres -c "psql -d aiden-db -c 'CREATE EXTENSION IF NOT EXISTS timescaledb;'"
su - postgres -c "psql -d aiden-db -c 'CREATE EXTENSION IF NOT EXISTS cstore_fdw;'"
su - postgres -c "psql -d aiden-db -c 'CREATE EXTENSION IF NOT EXISTS pgvector;'"

# Keep PostgreSQL running
exec "$@"
