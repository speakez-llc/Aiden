#!/bin/bash
set -e

# Assuming $PGDATA is the default data directory where postgres expects its configuration
# Append your custom configuration options to postgresql.conf
echo "shared_preload_libraries = 'agensgraph'" >> $PGDATA/postgresql.conf
echo "local_preload_libraries = 'pgcrypto'" >> $PGDATA/postgresql.conf
