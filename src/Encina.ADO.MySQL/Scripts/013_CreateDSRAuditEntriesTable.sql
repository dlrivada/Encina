-- =============================================
-- Create DSRAuditEntries table
-- For GDPR Data Subject Rights audit trail
-- =============================================

CREATE TABLE `DSRAuditEntries` (
    `Id`                VARCHAR(36)  NOT NULL PRIMARY KEY,
    `DSRRequestId`      VARCHAR(36)  NOT NULL,
    `Action`            VARCHAR(256) NOT NULL,
    `Detail`            TEXT         NULL,
    `PerformedByUserId` VARCHAR(256) NULL,
    `OccurredAtUtc`     DATETIME(6)  NOT NULL,
    INDEX `IX_DSRAuditEntries_DSRRequestId` (`DSRRequestId`),
    INDEX `IX_DSRAuditEntries_OccurredAtUtc` (`OccurredAtUtc`)
);
