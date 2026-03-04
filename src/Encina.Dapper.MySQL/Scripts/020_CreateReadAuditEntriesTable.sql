CREATE TABLE IF NOT EXISTS `ReadAuditEntries` (
    `Id`              CHAR(36)      NOT NULL,
    `EntityType`      VARCHAR(256)  NOT NULL,
    `EntityId`        VARCHAR(256)  NULL,
    `UserId`          VARCHAR(256)  NULL,
    `TenantId`        VARCHAR(128)  NULL,
    `AccessedAtUtc`   DATETIME(6)   NOT NULL,
    `CorrelationId`   VARCHAR(256)  NULL,
    `Purpose`         VARCHAR(1024) NULL,
    `AccessMethod`    INT           NOT NULL DEFAULT 0,
    `EntityCount`     INT           NOT NULL DEFAULT 0,
    `Metadata`        LONGTEXT      NULL,

    PRIMARY KEY (`Id`),
    INDEX `IX_ReadAuditEntries_Entity` (`EntityType`, `EntityId`),
    INDEX `IX_ReadAuditEntries_AccessedAt` (`AccessedAtUtc`),
    INDEX `IX_ReadAuditEntries_UserId` (`UserId`),
    INDEX `IX_ReadAuditEntries_TenantId` (`TenantId`),
    INDEX `IX_ReadAuditEntries_CorrelationId` (`CorrelationId`),
    INDEX `IX_ReadAuditEntries_AccessMethod` (`AccessMethod`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
