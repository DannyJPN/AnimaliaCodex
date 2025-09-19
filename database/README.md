# PZI - Database

## Overview

Database related scripts and utilities for PZI project.

## PostgreSQL schema

- The canonical schema definition lives in `new-schema/db/migrations/V0_0_1__initial_setup.sql` and targets PostgreSQL.
- Apply the script using `psql` against an empty `pzi` database:
  - `psql postgresql://pzi:STRONG_PASSWORD@localhost:5432/pzi -f new-schema/db/migrations/V0_0_1__initial_setup.sql`
- Sample data for development is available under `new-schema/db/sample-data`. Each script uses PostgreSQL syntax and can be executed via `psql`.

## Running PostgreSQL locally

- Use the docker compose file from the repository root to provision a local database:
  - `docker compose -f ../docker-compose.postgres.yml up -d`
- Default credentials are defined inside the compose file (`pzi`/`pzi`). Adjust as needed for your environment.
