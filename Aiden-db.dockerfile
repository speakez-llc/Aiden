# Use a Debian-based Postgres image as the base
FROM postgres:13

# Perform all server setup first before putting on Postgres

# Install build-essential tools and required packages
RUN apt-get update && \
    apt-get install -y build-essential wget git jq cmake protobuf-c-compiler libprotobuf-c-dev postgresql-server-dev-13 libkrb5-dev && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy the settings.json file from the Docker build context to the image
COPY settings.json /tmp/settings.json

# Extract the token from settings.json and set it as an environment variable
RUN export GITHUB_PAT=$(jq -r .token /tmp/settings.json)

# Clone and build Citus Columnstore extension
RUN git clone https://$GITHUB_PAT@github.com/citusdata/cstore_fdw.git /tmp/cstore_fdw && \
    cd /tmp/cstore_fdw && \
    PATH=/usr/local/pgsql/bin/:$PATH make && \
    PATH=/usr/local/pgsql/bin/:$PATH make install

# Clone TimescaleDB source code from GitHub
RUN git clone https://github.com/timescale/timescaledb.git /tmp/timescaledb && \
    cd /tmp/timescaledb && \
    git checkout 2.5.0

# Build and install TimescaleDB
RUN cd /tmp/timescaledb && \
    ./bootstrap -DREGRESS_CHECKS=OFF && \
    cd build && \
    cmake .. && \
    make && make install

# Clone pgvector source code from GitHub
RUN git clone https://$GITHUB_PAT@github.com/pgvector/pgvector.git /tmp/pgvector && \
    cd /tmp/pgvector && \
    make && make install INSTALLDIR=/usr/share/postgresql/13/extension

# Specify the extensions in the PostgreSQL configuration
RUN echo "shared_preload_libraries = 'timescaledb,cstore_fdw,vector'" >> /usr/share/postgresql/postgresql.conf.sample

# Create a custom PostgreSQL configuration file
COPY postgresql.custom.conf /etc/postgresql/postgresql.conf

# Ensure that the custom configuration is loaded
RUN echo "include '/etc/postgresql/postgresql.conf'" >> /usr/share/postgresql/postgresql.conf.sample

# Initialize a PostgreSQL cluster
RUN /etc/init.d/postgresql start && \
    su - postgres -c "pg_createcluster 13 main" && \
    /etc/init.d/postgresql stop

# Copy the init-db.sh script to the container
COPY init-db.sh /usr/local/bin/

# Make the script executable
RUN chmod +x /usr/local/bin/init-db.sh

# Set the script as the entrypoint
ENTRYPOINT ["init-db.sh"]

# Start PostgreSQL cluster
CMD ["postgres", "-c", "config_file=/etc/postgresql/postgresql.conf"]
