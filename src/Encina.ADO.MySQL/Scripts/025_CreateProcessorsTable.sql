CREATE TABLE `Processors` (
    `Id`                                VARCHAR(256)  NOT NULL PRIMARY KEY,
    `Name`                              VARCHAR(450)  NOT NULL,
    `Country`                           VARCHAR(100)  NOT NULL,
    `ContactEmail`                      VARCHAR(256)  NULL,
    `ParentProcessorId`                 VARCHAR(256)  NULL,
    `Depth`                             INT           NOT NULL,
    `SubProcessorAuthorizationTypeValue` INT          NOT NULL,
    `TenantId`                          VARCHAR(256)  NULL,
    `ModuleId`                          VARCHAR(256)  NULL,
    `CreatedAtUtc`                      DATETIME(6)   NOT NULL,
    `LastUpdatedAtUtc`                  DATETIME(6)   NOT NULL,
    INDEX `IX_Processors_ParentProcessorId` (`ParentProcessorId`),
    INDEX `IX_Processors_TenantId` (`TenantId`)
);
