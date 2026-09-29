#!/bin/bash
# Roda uma única vez, na primeira subida do contêiner do Postgres.
# O POSTGRES_DB do compose cria o lingol_cadastro; este script cria o segundo.
set -e
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname lingol_cadastro <<-EOSQL
    CREATE DATABASE lingol_pedagogico;
EOSQL
