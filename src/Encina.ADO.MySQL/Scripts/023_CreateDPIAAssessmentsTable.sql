CREATE TABLE `DPIAAssessments` (
    `Id`                  VARCHAR(36)   NOT NULL PRIMARY KEY,
    `RequestTypeName`     VARCHAR(450)  NOT NULL,
    `StatusValue`         INT           NOT NULL,
    `ProcessingType`      VARCHAR(256)  NULL,
    `Reason`              TEXT          NULL,
    `ResultJson`          JSON          NULL,
    `DPOConsultationJson` JSON          NULL,
    `CreatedAtUtc`        DATETIME(6)   NOT NULL,
    `ApprovedAtUtc`       DATETIME(6)   NULL,
    `NextReviewAtUtc`     DATETIME(6)   NULL,
    `TenantId`            VARCHAR(256)  NULL,
    `ModuleId`            VARCHAR(256)  NULL,
    UNIQUE INDEX `UX_DPIAAssessments_RequestTypeName` (`RequestTypeName`),
    INDEX `IX_DPIAAssessments_StatusValue` (`StatusValue`),
    INDEX `IX_DPIAAssessments_NextReviewAtUtc` (`NextReviewAtUtc`)
);
