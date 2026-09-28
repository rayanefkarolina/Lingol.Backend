#!/bin/bash
# Roda uma única vez, quando o volume do Postgres é criado.
# O POSTGRES_DB do compose já cria o lingol_cadastro; aqui fica o segundo.
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "postgres" <<-SQL
    CREATE DATABASE lingol_pedagogico OWNER $POSTGRES_USER;
SQL

echo "Banco lingol_pedagogico criado."
