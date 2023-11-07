# Use a Debian-based Postgres image as the base
FROM postgres:15

# Install build-essential tools and required packages
RUN apt-get update && \
    apt-get install -y \
    build-essential \
    gcc \
    g++ \
    libreadline-dev \
    zlib1g-dev \
    bison \
    flex \
    make \
    libbz2-dev \
    zlib1g-dev \
    curl \
    wget \
    fontconfig \
    git \
    jq \
    autoconf \
    automake \
    cmake \
    gnupg \
    postgresql-common \
    apt-transport-https \
    lsb-release \
    protobuf-c-compiler \
    libprotobuf-c-dev \
    postgresql-server-dev-15 \
    libkrb5-dev && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Install Fira Code Nerd Font to PowerShell
RUN mkdir -p ~/.local/share/fonts && \
    cd ~/.local/share/fonts && curl -fLO https://github.com/ryanoasis/nerd-fonts/raw/HEAD/patched-fonts/FiraCode/Regular/FiraCodeNerdFont-Regular.ttf

RUN fc-cache -fv

# Clone timescaledb source code from GitHub and install
#RUN git clone https://github.com/timescale/timescaledb && \
#    cd timescaledb && \
#    git checkout 2.12.2 && \
#    ./bootstrap && \
#    cd build && make && \
#    make install

# Clone pgvector source code from GitHub and install
#RUN cd /tmp && \
#    git clone --branch v0.5.1 https://github.com/pgvector/pgvector.git && \
#    cd pgvector && \
#    make && \
#    make install

# Clone AgensGraph source from GitHub and install
RUN git clone https://github.com/bitnine-oss/agensgraph.git && \
    cd agensgraph && \
    ./configure --prefix=/usr/local/agensgraph && \
    make && \
    make install

ENV LD_LIBRARY_PATH=/usr/local/agensgraph/lib:$LD_LIBRARY_PATH
ENV PATH=/usr/local/agensgraph/bin:$PATH

# Clone and build Citus Columnstore extension
#RUN curl https://install.citusdata.com/community/deb.sh > add-citus-repo.sh && \
#    bash add-citus-repo.sh && \
#    apt-get -y install postgresql-15-citus-12.1

# Clone the bgpdump repository from GitHub and install
RUN git clone https://github.com/RIPE-NCC/bgpdump.git && \
    cd bgpdump && \
    ./bootstrap.sh && \
    ./configure && \
    make && \
    make install

# Create a custom PostgreSQL configuration file
COPY postgresql.custom.conf /etc/postgresql/postgresql.conf

# Initialize a PostgreSQL cluster
RUN /etc/init.d/postgresql start && \
    su - postgres -c "pg_createcluster 15 main" && \
    /etc/init.d/postgresql stop

# Copy the custom PostgreSQL configuration file from the local directory to the container
COPY postgresql.custom.conf /etc/postgresql/15/main/postgresql.custom.conf

# Specify the extensions in the PostgreSQL configuration
RUN echo "shared_preload_libraries = 'agensgraph'"  >> /etc/postgresql/15/main/postgresql.custom.conf && \
    echo "local_preload_libraries = 'pgcrypto'" >> /etc/postgresql/15/main/postgresql.custom.conf

RUN echo "include = '/etc/postgresql/15/main/postgresql.custom.conf'" >> /etc/postgresql/15/main/postgresql.conf

# Copy the SQL script to the initialization directory
COPY create_db.sql /docker-entrypoint-initdb.d/

# Set the script as the entrypoint
ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh"]

# Start PostgreSQL cluster
CMD ["postgres", "-c", "config_file=/etc/postgresql/postgresql.conf"]
