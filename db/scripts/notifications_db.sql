CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260930000000_InitialCreate') THEN
    CREATE TABLE notifications (
        id uuid NOT NULL,
        message_id uuid NOT NULL,
        message_type character varying(100) NOT NULL,
        event_id uuid NOT NULL,
        event_name character varying(150) NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        correlation_id uuid NOT NULL,
        payload_hash character(64) NOT NULL,
        payload jsonb NOT NULL,
        channel character varying(20) NOT NULL,
        status character varying(20) NOT NULL,
        attempts integer NOT NULL,
        last_error character varying(1000),
        received_at timestamp with time zone NOT NULL,
        sent_at timestamp with time zone,
        CONSTRAINT pk_notifications PRIMARY KEY (id),
        CONSTRAINT ck_notifications_status CHECK (status IN ('Pending', 'Sent', 'Failed'))
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260930000000_InitialCreate') THEN
    CREATE INDEX ix_notifications_event_id ON notifications (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260930000000_InitialCreate') THEN
    CREATE INDEX ix_notifications_status ON notifications (status) WHERE status <> 'Sent';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260930000000_InitialCreate') THEN
    CREATE UNIQUE INDEX uq_notifications_message_id ON notifications (message_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260930000000_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260930000000_InitialCreate', '10.0.12');
    END IF;
END $EF$;
COMMIT;

