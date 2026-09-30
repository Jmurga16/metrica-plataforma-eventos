\set ON_ERROR_STOP on

-- The Docker Postgres entrypoint runs this script only when the data volume is empty.
-- Passwords match the fictitious local defaults in .env.example.
DO
$$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'events_app') THEN
        CREATE ROLE events_app LOGIN PASSWORD 'events-local-password';
    END IF;

    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'notifications_app') THEN
        CREATE ROLE notifications_app LOGIN PASSWORD 'notifications-local-password';
    END IF;
END
$$;

SELECT 'CREATE DATABASE events_db OWNER events_app'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'events_db')\gexec

SELECT 'CREATE DATABASE notifications_db OWNER notifications_app'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'notifications_db')\gexec

REVOKE CONNECT ON DATABASE events_db FROM PUBLIC;
REVOKE CONNECT ON DATABASE notifications_db FROM PUBLIC;
GRANT CONNECT, TEMPORARY ON DATABASE events_db TO events_app;
GRANT CONNECT, TEMPORARY ON DATABASE notifications_db TO notifications_app;

