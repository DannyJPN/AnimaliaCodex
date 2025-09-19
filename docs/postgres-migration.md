# PostgreSQL migration playbook

## Overview

This document describes the approach for migrating the Animalia Codex data platform from Microsoft SQL Server to PostgreSQL.

## 1. Provision the database

1. Start the PostgreSQL container provided in `docker-compose.postgres.yml`:
   - `docker compose -f docker-compose.postgres.yml up -d`
2. Verify connectivity: `psql postgresql://pzi:pzi@localhost:5432/pzi -c 'select 1;'`.

## 2. Apply the schema

1. Execute the canonical schema script against the freshly created database:
   - `psql postgresql://pzi:STRONG_PASSWORD@localhost:5432/pzi -f database/new-schema/db/migrations/V0_0_1__initial_setup.sql`
2. Load optional seed data for development:
   - `psql postgresql://pzi:STRONG_PASSWORD@localhost:5432/pzi -f database/new-schema/db/sample-data/D0_0_1__journal_bio_data.sql`

## 3. Data migration strategy

1. Export source data from the legacy SQL Server database using a tool such as `pgloader` or the provided export utility.
2. Normalize exported CSV files to UTF-8 and adjust column names to match the PostgreSQL schema.
3. Use PostgreSQL `COPY` commands or `psql \copy` to load the data into staging tables that mirror the target entities.
4. Run the data enrichment scripts (quantities recalculation, placement mappings) against the staging tables before switching data into production tables.

## 4. Validation steps

1. Run application smoke tests (API, web app) against the PostgreSQL instance.
2. Execute regression queries to compare row counts and key aggregates between SQL Server and PostgreSQL.
3. Inspect critical workflows (authentication, CRUD operations, reporting) in a staging environment backed by PostgreSQL.

## 5. Rollback plan

1. Retain the SQL Server environment until PostgreSQL is proven stable.
2. Keep a recent SQL Server backup that can be restored if blocking defects are discovered during cut-over.
3. Document any manual adjustments performed during the migration so they can be re-applied if rollback occurs.

