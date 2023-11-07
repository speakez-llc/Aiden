# Use a Debian-based Postgres image as the base
FROM postgres:15

# Install build-essential tools and required packages
RUN apt-get update && \
    apt-get install -y build-essential curl wget git jq cmake gnupg postgresql-common apt-transport-https lsb-release protobuf-c-compiler libprotobuf-c-dev postgresql-server-dev-15 libkrb5-dev && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Clone timescaledb source code from GitHub
RUN git clone https://github.com/timescale/timescaledb && \
    cd timescaledb && \
    git checkout 2.12.2 && \
    ./bootstrap && \
    cd build && make && \
    make install

RUN cd /tmp && \
    git clone --branch v0.5.1 https://github.com/pgvector/pgvector.git && \
    cd pgvector && \
    make && \
    make install

# Specify the extensions in the PostgreSQL configuration
RUN echo "shared_preload_libraries = 'timescaledb,vector'" >> /usr/share/postgresql/postgresql.custom.conf

# Add pgcrypto to PostgreSQL configuration
# RUN echo "local_preload_libraries = 'pgcrypto'" >> /usr/share/postgresql/postgresql.custom.conf


# Create a custom PostgreSQL configuration file
COPY postgresql.custom.conf /etc/postgresql/postgresql.conf

# Copy the SQL script to the initialization directory
COPY create_db.sql /docker-entrypoint-initdb.d/

# Set the script as the entrypoint
ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh"]

# Start PostgreSQL cluster
CMD ["postgres", "-c", "config_file=/etc/postgresql/postgresql.conf"]
