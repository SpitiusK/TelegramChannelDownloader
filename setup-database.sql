-- PostgreSQL Setup Script for Telegram Channel Downloader
-- Run this script after installing PostgreSQL
-- Execute as postgres superuser: psql -U postgres -h localhost -f setup-database.sql

-- Create application user
CREATE USER telegram_user WITH PASSWORD 'secure_password_123';

-- Create application database
CREATE DATABASE telegram_downloads OWNER telegram_user;

-- Grant necessary privileges
GRANT ALL PRIVILEGES ON DATABASE telegram_downloads TO telegram_user;

-- Connect to the new database
\c telegram_downloads;

-- Grant schema privileges
GRANT ALL ON SCHEMA public TO telegram_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO telegram_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO telegram_user;
GRANT ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA public TO telegram_user;

-- Set default privileges for future objects
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO telegram_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO telegram_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON FUNCTIONS TO telegram_user;

-- Enable UUID extension (needed for DownloadSession IDs)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create additional extensions that might be useful for performance
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";

-- Enable trigram extension for future full-text search capabilities
-- (Currently not used in initial migration but available for future features)
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Verify setup
SELECT current_database(), current_user;

-- Display created user and database
\du telegram_user
\l telegram_downloads

-- Success message
\echo 'Database setup completed successfully!'
\echo 'Database: telegram_downloads'
\echo 'User: telegram_user'
\echo 'Password: secure_password_123'