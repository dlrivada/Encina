-- =============================================
-- Encina.ADO.Sqlite - Complete Database Schema
-- Run this script to create all messaging pattern tables
-- =============================================

-- =============================================
-- OutboxMessages - Reliable Event Publishing
-- =============================================
CREATE TABLE IF NOT EXISTS OutboxMessages
(
    Id TEXT NOT NULL PRIMARY KEY,
    NotificationType TEXT NOT NULL,
    Content TEXT NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    ProcessedAtUtc TEXT NULL,
    ErrorMessage TEXT NULL,
    RetryCount INTEGER NOT NULL DEFAULT 0,
    NextRetryAtUtc TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_OutboxMessages_ProcessedAt
    ON OutboxMessages (ProcessedAtUtc, RetryCount, NextRetryAtUtc);

-- =============================================
-- InboxMessages - Idempotent Message Processing
-- =============================================
CREATE TABLE IF NOT EXISTS InboxMessages
(
    MessageId TEXT NOT NULL PRIMARY KEY,
    RequestType TEXT NOT NULL,
    ReceivedAtUtc TEXT NOT NULL,
    ProcessedAtUtc TEXT NULL,
    ExpiresAtUtc TEXT NOT NULL,
    Response TEXT NULL,
    ErrorMessage TEXT NULL,
    RetryCount INTEGER NOT NULL DEFAULT 0,
    NextRetryAtUtc TEXT NULL,
    Metadata TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_InboxMessages_ExpiresAt
    ON InboxMessages (ExpiresAtUtc);

-- =============================================
-- SagaStates - Distributed Transaction Orchestration
-- Status values: 0=Running, 1=Completed, 2=Failed, 3=Compensating, 4=Compensated
-- =============================================
CREATE TABLE IF NOT EXISTS SagaStates
(
    SagaId TEXT NOT NULL PRIMARY KEY,
    SagaType TEXT NOT NULL,
    Data TEXT NOT NULL,
    Status INTEGER NOT NULL,
    StartedAtUtc TEXT NOT NULL,
    LastUpdatedAtUtc TEXT NOT NULL,
    CompletedAtUtc TEXT NULL,
    ErrorMessage TEXT NULL,
    CurrentStep INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS IX_SagaStates_Status
    ON SagaStates (Status, LastUpdatedAtUtc);

-- =============================================
-- ScheduledMessages - Delayed/Recurring Execution
-- =============================================
CREATE TABLE IF NOT EXISTS ScheduledMessages
(
    Id TEXT NOT NULL PRIMARY KEY,
    RequestType TEXT NOT NULL,
    Content TEXT NOT NULL,
    ScheduledAtUtc TEXT NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    ProcessedAtUtc TEXT NULL,
    LastExecutedAtUtc TEXT NULL,
    ErrorMessage TEXT NULL,
    RetryCount INTEGER NOT NULL DEFAULT 0,
    NextRetryAtUtc TEXT NULL,
    IsRecurring INTEGER NOT NULL DEFAULT 0,
    CronExpression TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_ScheduledMessages_ScheduledAt
    ON ScheduledMessages (ScheduledAtUtc, ProcessedAtUtc, RetryCount);

-- Encina.ADO.Sqlite schema installation complete!
-- You can now use all messaging patterns with ADO.NET.
