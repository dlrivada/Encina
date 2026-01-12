-- =============================================
-- Encina.ADO.Oracle - Complete Database Schema
-- Run this script to create all messaging pattern tables
-- =============================================

-- =============================================
-- OutboxMessages - Reliable Event Publishing
-- =============================================
DECLARE
    table_exists NUMBER;
BEGIN
    SELECT COUNT(*) INTO table_exists FROM user_tables WHERE table_name = 'OUTBOXMESSAGES';
    IF table_exists = 0 THEN
        EXECUTE IMMEDIATE 'CREATE TABLE OutboxMessages (Id RAW(16) NOT NULL PRIMARY KEY, NotificationType VARCHAR2(500) NOT NULL, Content CLOB NOT NULL, CreatedAtUtc TIMESTAMP NOT NULL, ProcessedAtUtc TIMESTAMP NULL, ErrorMessage CLOB NULL, RetryCount NUMBER(10) DEFAULT 0 NOT NULL, NextRetryAtUtc TIMESTAMP NULL)';
        EXECUTE IMMEDIATE 'CREATE INDEX IX_OutboxMessages_ProcessedAt ON OutboxMessages (ProcessedAtUtc, RetryCount, NextRetryAtUtc)';
        DBMS_OUTPUT.PUT_LINE('Created table: OutboxMessages');
    ELSE
        DBMS_OUTPUT.PUT_LINE('Table already exists: OutboxMessages');
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Error creating OutboxMessages: ' || SQLERRM);
        RAISE;
END;
/

-- =============================================
-- InboxMessages - Idempotent Message Processing
-- =============================================
DECLARE
    table_exists NUMBER;
BEGIN
    SELECT COUNT(*) INTO table_exists FROM user_tables WHERE table_name = 'INBOXMESSAGES';
    IF table_exists = 0 THEN
        EXECUTE IMMEDIATE 'CREATE TABLE InboxMessages (MessageId VARCHAR2(255) NOT NULL PRIMARY KEY, RequestType VARCHAR2(500) NOT NULL, ReceivedAtUtc TIMESTAMP NOT NULL, ProcessedAtUtc TIMESTAMP NULL, ExpiresAtUtc TIMESTAMP NOT NULL, Response CLOB NULL, ErrorMessage CLOB NULL, RetryCount NUMBER(10) DEFAULT 0 NOT NULL, NextRetryAtUtc TIMESTAMP NULL, Metadata CLOB NULL)';
        EXECUTE IMMEDIATE 'CREATE INDEX IX_InboxMessages_ExpiresAt ON InboxMessages (ExpiresAtUtc)';
        DBMS_OUTPUT.PUT_LINE('Created table: InboxMessages');
    ELSE
        DBMS_OUTPUT.PUT_LINE('Table already exists: InboxMessages');
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Error creating InboxMessages: ' || SQLERRM);
        RAISE;
END;
/

-- =============================================
-- SagaStates - Distributed Transaction Orchestration
-- Status: 0=Running, 1=Completed, 2=Failed, 3=Compensating, 4=Compensated
-- =============================================
DECLARE
    table_exists NUMBER;
BEGIN
    SELECT COUNT(*) INTO table_exists FROM user_tables WHERE table_name = 'SAGASTATES';
    IF table_exists = 0 THEN
        EXECUTE IMMEDIATE 'CREATE TABLE SagaStates (SagaId RAW(16) NOT NULL PRIMARY KEY, SagaType VARCHAR2(500) NOT NULL, Data CLOB NOT NULL, Status NUMBER(10) NOT NULL, StartedAtUtc TIMESTAMP NOT NULL, LastUpdatedAtUtc TIMESTAMP NOT NULL, CompletedAtUtc TIMESTAMP NULL, ErrorMessage CLOB NULL, CurrentStep NUMBER(10) DEFAULT 0 NOT NULL)';
        EXECUTE IMMEDIATE 'CREATE INDEX IX_SagaStates_Status ON SagaStates (Status, LastUpdatedAtUtc)';
        DBMS_OUTPUT.PUT_LINE('Created table: SagaStates');
    ELSE
        DBMS_OUTPUT.PUT_LINE('Table already exists: SagaStates');
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Error creating SagaStates: ' || SQLERRM);
        RAISE;
END;
/

-- =============================================
-- ScheduledMessages - Delayed/Recurring Execution
-- =============================================
DECLARE
    table_exists NUMBER;
BEGIN
    SELECT COUNT(*) INTO table_exists FROM user_tables WHERE table_name = 'SCHEDULEDMESSAGES';
    IF table_exists = 0 THEN
        EXECUTE IMMEDIATE 'CREATE TABLE ScheduledMessages (Id RAW(16) NOT NULL PRIMARY KEY, RequestType VARCHAR2(500) NOT NULL, Content CLOB NOT NULL, ScheduledAtUtc TIMESTAMP NOT NULL, CreatedAtUtc TIMESTAMP NOT NULL, ProcessedAtUtc TIMESTAMP NULL, LastExecutedAtUtc TIMESTAMP NULL, ErrorMessage CLOB NULL, RetryCount NUMBER(10) DEFAULT 0 NOT NULL, NextRetryAtUtc TIMESTAMP NULL, IsRecurring NUMBER(1) DEFAULT 0 NOT NULL, CronExpression VARCHAR2(100) NULL)';
        EXECUTE IMMEDIATE 'CREATE INDEX IX_ScheduledMessages_ScheduledAt ON ScheduledMessages (ScheduledAtUtc, ProcessedAtUtc, RetryCount)';
        DBMS_OUTPUT.PUT_LINE('Created table: ScheduledMessages');
    ELSE
        DBMS_OUTPUT.PUT_LINE('Table already exists: ScheduledMessages');
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Error creating ScheduledMessages: ' || SQLERRM);
        RAISE;
END;
/

BEGIN
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('Encina.ADO.Oracle schema installation complete!');
    DBMS_OUTPUT.PUT_LINE('You can now use all messaging patterns with ADO.NET.');
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Error: ' || SQLERRM);
        RAISE;
END;
/
