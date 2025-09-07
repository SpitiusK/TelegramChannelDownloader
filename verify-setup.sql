-- Verification script for PostgreSQL setup
-- Run this after installation: psql -U telegram_user -d telegram_downloads -h localhost -f verify-setup.sql

-- Display current connection info
SELECT 
    current_database() as database_name,
    current_user as connected_user,
    inet_server_addr() as server_address,
    inet_server_port() as server_port;

-- Check if required extensions are installed
SELECT 
    extname as extension_name,
    extversion as version
FROM pg_extension 
WHERE extname IN ('uuid-ossp', 'pg_stat_statements');

-- List all tables created by Entity Framework
SELECT 
    schemaname,
    tablename,
    tableowner
FROM pg_tables 
WHERE schemaname = 'public'
ORDER BY tablename;

-- Check table structures
\echo '=== DOWNLOAD_SESSIONS TABLE STRUCTURE ==='
\d download_sessions

\echo '=== TELEGRAM_MESSAGE TABLE STRUCTURE ==='
\d telegram_message

-- Check indexes
SELECT 
    tablename,
    indexname,
    indexdef
FROM pg_indexes 
WHERE schemaname = 'public'
ORDER BY tablename, indexname;

-- Check foreign key constraints
SELECT 
    tc.table_schema,
    tc.table_name,
    kcu.column_name,
    ccu.table_schema AS foreign_table_schema,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
    AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
    AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
    AND tc.table_schema = 'public';

-- Display database size
SELECT 
    pg_database.datname as database_name,
    pg_size_pretty(pg_database_size(pg_database.datname)) AS size
FROM pg_database
WHERE datname = 'telegram_downloads';

\echo '=== SETUP VERIFICATION COMPLETE ==='
\echo 'If you see tables and indexes listed above, your setup is successful!'