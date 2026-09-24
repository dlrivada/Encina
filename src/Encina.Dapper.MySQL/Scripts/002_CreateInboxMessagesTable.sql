-- =============================================
-- Create InboxMessages table
-- For idempotent message processing (exactly-once semantics)
-- =============================================
-- Note: MySQL has no partial/filtered index equivalent to SQL Server's
-- "WHERE ProcessedAtUtc IS NOT NULL"; ExpiresAtUtc is indexed unconditionally.

CREATE TABLE IF NOT EXISTS `InboxMessages`
(
    `MessageId` VARCHAR(255) NOT NULL,
    `RequestType` VARCHAR(500) NOT NULL,
    `ReceivedAtUtc` DATETIME(6) NOT NULL,
    `ProcessedAtUtc` DATETIME(6) NULL,
    `ExpiresAtUtc` DATETIME(6) NOT NULL,
    `Response` LONGTEXT NULL,
    `ErrorMessage` LONGTEXT NULL,
    `RetryCount` INT NOT NULL DEFAULT 0,
    `NextRetryAtUtc` DATETIME(6) NULL,
    `Metadata` LONGTEXT NULL,
    PRIMARY KEY (`MessageId`),
    INDEX `IX_InboxMessages_ExpiresAt` (`ExpiresAtUtc`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
