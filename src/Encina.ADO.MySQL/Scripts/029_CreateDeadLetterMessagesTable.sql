-- =============================================
-- DeadLetterMessages - Dead Letter Queue
-- Messages that failed for good, kept for inspection, replay and expiry.
-- Lengths come from Encina.Messaging.DeadLetter.DeadLetterStoreLimits.
-- =============================================

CREATE TABLE IF NOT EXISTS `DeadLetterMessages` (
    `Id`                  CHAR(36)       NOT NULL,
    `RequestType`         VARCHAR(1000)  NOT NULL,
    `RequestContent`      LONGTEXT       NOT NULL,
    `ErrorCode`           VARCHAR(256)   NOT NULL,
    `ExceptionType`       VARCHAR(512)   NULL,
    `ExceptionStackTrace` LONGTEXT       NULL,
    `CorrelationId`       VARCHAR(256)   NULL,
    `SourcePattern`       VARCHAR(64)    NOT NULL,
    `SourceMessageId`     VARCHAR(256)   NOT NULL,
    `TenantId`            VARCHAR(128)   NULL,
    `TotalRetryAttempts`  INT            NOT NULL,
    `FirstFailedAtUtc`    DATETIME(6)    NOT NULL,
    `DeadLetteredAtUtc`   DATETIME(6)    NOT NULL,
    `ExpiresAtUtc`        DATETIME(6)    NULL,
    `ReplayClaimedAtUtc`  DATETIME(6)    NULL,
    `ReplayedAtUtc`       DATETIME(6)    NULL,
    `ReplayResult`        VARCHAR(256)   NULL,

    PRIMARY KEY (`Id`),
    UNIQUE INDEX `UX_DeadLetterMessages_Source` (`SourcePattern`, `SourceMessageId`),
    INDEX `IX_DeadLetterMessages_DeadLetteredAt` (`DeadLetteredAtUtc`, `Id`),
    INDEX `IX_DeadLetterMessages_Pending` (`ReplayedAtUtc`, `SourcePattern`, `DeadLetteredAtUtc`),
    INDEX `IX_DeadLetterMessages_ExpiresAt` (`ExpiresAtUtc`),
    INDEX `IX_DeadLetterMessages_CorrelationId` (`CorrelationId`),
    INDEX `IX_DeadLetterMessages_Tenant` (`TenantId`, `DeadLetteredAtUtc`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
