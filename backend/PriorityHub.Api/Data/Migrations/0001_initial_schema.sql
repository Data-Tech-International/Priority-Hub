-- Migration 0001: initial schema
-- Creates the schema_migrations tracking table and the user_config JSONB table.

CREATE TABLE IF NOT EXISTS schema_migrations (
    version     INTEGER     PRIMARY KEY,
    applied_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS user_config (
    user_id     TEXT        PRIMARY KEY,
    config      JSONB       NOT NULL DEFAULT '{}',
    version     INTEGER     NOT NULL DEFAULT 1,
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
