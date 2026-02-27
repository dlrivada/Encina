-- =============================================
-- Create DSRRequests table
-- For GDPR Data Subject Rights (Articles 15-22)
-- =============================================

CREATE TABLE `DSRRequests` (
    `Id`                    VARCHAR(36)   NOT NULL PRIMARY KEY,
    `SubjectId`             VARCHAR(256)  NOT NULL,
    `RightTypeValue`        INT           NOT NULL,
    `StatusValue`           INT           NOT NULL,
    `ReceivedAtUtc`         DATETIME(6)   NOT NULL,
    `DeadlineAtUtc`         DATETIME(6)   NOT NULL,
    `CompletedAtUtc`        DATETIME(6)   NULL,
    `ExtensionReason`       VARCHAR(1024) NULL,
    `ExtendedDeadlineAtUtc` DATETIME(6)   NULL,
    `RejectionReason`       VARCHAR(1024) NULL,
    `RequestDetails`        TEXT          NULL,
    `VerifiedAtUtc`         DATETIME(6)   NULL,
    `ProcessedByUserId`     VARCHAR(256)  NULL,
    INDEX `IX_DSRRequests_SubjectId` (`SubjectId`),
    INDEX `IX_DSRRequests_StatusValue` (`StatusValue`),
    INDEX `IX_DSRRequests_RightTypeValue` (`RightTypeValue`),
    INDEX `IX_DSRRequests_DeadlineAtUtc` (`DeadlineAtUtc`)
);
