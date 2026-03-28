-- Migration 0001: initial schema
-- Creates the user_config JSONB table for per-user provider configuration.

CREATE TABLE IF NOT EXISTS user_config (
    user_id     TEXT        PRIMARY KEY,
    config      JSONB       NOT NULL DEFAULT '{}',
    version     INTEGER     NOT NULL DEFAULT 1,
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
