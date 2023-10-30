#!/bin/bash
set -e

# Start PostgreSQL as an unprivileged user
gosu postgres /usr/local/bin/docker-entrypoint.sh postgres

# Wait for PostgreSQL to become ready
until pg_isready -h localhost -p 5432 -U postgres
do
    echo "Waiting for PostgreSQL to start..."
    sleep 2
done

# Create a database
gosu postgres createdb aiden-db

# Connect to the database and add extensions
gosu postgres psql -d aiden-db -c 'CREATE EXTENSION IF NOT EXISTS timescaledb;'
gosu postgres psql -d aiden-db -c 'CREATE EXTENSION IF NOT EXISTS cstore_fdw;'
gosu postgres psql -d aiden-db -c 'CREATE EXTENSION IF NOT EXISTS vector;'

# Keep PostgreSQL running
exec "$@"
