CREATE TABLE `ResidencyPolicies` (
    `DataCategory`              VARCHAR(256)  NOT NULL PRIMARY KEY,
    `AllowedRegionCodes`        VARCHAR(1024) NOT NULL,
    `RequireAdequacyDecision`   TINYINT(1)    NOT NULL,
    `AllowedTransferBasesValue` VARCHAR(256)  NULL,
    `CreatedAtUtc`              DATETIME(6)   NOT NULL,
    `LastModifiedAtUtc`         DATETIME(6)   NULL
);
