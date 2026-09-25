-- =============================================
-- Encina.ADO - Complete Database Schema (PostgreSQL)
-- Run this script to create all messaging pattern tables
-- =============================================

-- =============================================
-- Create OutboxMessages table
-- For reliable event publishing (at-least-once delivery)
-- =============================================

CREATE TABLE IF NOT EXISTS outboxmessages
(
    id UUID NOT NULL PRIMARY KEY,
    notificationtype TEXT NOT NULL,
    content TEXT NOT NULL,
    createdatutc TIMESTAMP NOT NULL,
    processedatutc TIMESTAMP NULL,
    errormessage TEXT NULL,
    retrycount INTEGER NOT NULL DEFAULT 0,
    nextretryatutc TIMESTAMP NULL
);

CREATE INDEX IF NOT EXISTS ix_outboxmessages_processedat_retrycount
    ON outboxmessages (processedatutc, retrycount, nextretryatutc)
    INCLUDE (createdatutc);

-- =============================================
-- Create InboxMessages table
-- For idempotent message processing (exactly-once semantics)
-- =============================================

CREATE TABLE IF NOT EXISTS inboxmessages (
    messageid TEXT NOT NULL PRIMARY KEY,
    requesttype TEXT NOT NULL,
    receivedatutc TIMESTAMP NOT NULL,
    processedatutc TIMESTAMP NULL,
    expiresatutc TIMESTAMP NOT NULL,
    response TEXT NULL,
    errormessage TEXT NULL,
    retrycount INTEGER NOT NULL DEFAULT 0,
    nextretryatutc TIMESTAMP NULL,
    metadata TEXT NULL
);

CREATE INDEX IF NOT EXISTS ix_inboxmessages_expiresat ON inboxmessages (expiresatutc) WHERE processedatutc IS NOT NULL;

-- =============================================
-- Create SagaStates table
-- For distributed transaction orchestration with compensation
-- =============================================

CREATE TABLE IF NOT EXISTS sagastates
(
    sagaid UUID NOT NULL PRIMARY KEY,
    sagatype TEXT NOT NULL,
    data TEXT NOT NULL,
    status VARCHAR(50) NOT NULL, -- Running, Completed, Failed, Compensating, Compensated
    startedatutc TIMESTAMP NOT NULL,
    lastupdatedatutc TIMESTAMP NOT NULL,
    completedatutc TIMESTAMP NULL,
    errormessage TEXT NULL,
    currentstep INTEGER NOT NULL DEFAULT 0,
    timeoutatutc TIMESTAMP NULL,
    correlationid VARCHAR(256) NULL,
    metadata TEXT NULL
);

CREATE INDEX IF NOT EXISTS ix_sagastates_status_lastupdated ON sagastates (status, lastupdatedatutc);

-- =============================================
-- Create ScheduledMessages table
-- For delayed and recurring command execution
-- =============================================

CREATE TABLE IF NOT EXISTS scheduledmessages
(
    id UUID NOT NULL PRIMARY KEY,
    requesttype VARCHAR(500) NOT NULL,
    content TEXT NOT NULL,
    scheduledatutc TIMESTAMP NOT NULL,
    createdatutc TIMESTAMP NOT NULL,
    processedatutc TIMESTAMP NULL,
    lastexecutedatutc TIMESTAMP NULL,
    errormessage TEXT NULL,
    retrycount INTEGER NOT NULL DEFAULT 0,
    nextretryatutc TIMESTAMP NULL,
    correlationid VARCHAR(256) NULL,
    metadata TEXT NULL,
    isrecurring BOOLEAN NOT NULL DEFAULT FALSE,
    cronexpression VARCHAR(100) NULL
);

CREATE INDEX IF NOT EXISTS ix_scheduledmessages_scheduledat_processed
    ON scheduledmessages (scheduledatutc, processedatutc, retrycount)
    INCLUDE (nextretryatutc, isrecurring);

-- =============================================
-- Create AuditLogs table for PostgreSQL
-- For audit trail tracking
-- =============================================

CREATE TABLE IF NOT EXISTS "AuditLogs"
(
    "Id" VARCHAR(256) NOT NULL,
    "EntityType" VARCHAR(256) NOT NULL,
    "EntityId" VARCHAR(256) NOT NULL,
    "Action" INTEGER NOT NULL,
    "UserId" VARCHAR(256) NULL,
    "TimestampUtc" TIMESTAMPTZ NOT NULL,
    "OldValues" TEXT NULL,
    "NewValues" TEXT NULL,
    "CorrelationId" VARCHAR(256) NULL,

    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id")
);

-- Composite index for efficient history lookups by entity
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Entity" ON "AuditLogs" ("EntityType", "EntityId");

-- Index for time-based queries
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Timestamp" ON "AuditLogs" ("TimestampUtc");

-- Partial index on UserId for user activity tracking (only non-null values)
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_UserId" ON "AuditLogs" ("UserId") WHERE "UserId" IS NOT NULL;

-- Partial index on CorrelationId for request correlation tracking (only non-null values)
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_CorrelationId" ON "AuditLogs" ("CorrelationId") WHERE "CorrelationId" IS NOT NULL;
