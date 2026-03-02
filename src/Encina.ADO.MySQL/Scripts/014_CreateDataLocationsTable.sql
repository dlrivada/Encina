CREATE TABLE `DataLocations` (
    `Id`                VARCHAR(36)   NOT NULL PRIMARY KEY,
    `EntityId`          VARCHAR(256)  NOT NULL,
    `DataCategory`      VARCHAR(256)  NOT NULL,
    `RegionCode`        VARCHAR(32)   NOT NULL,
    `StorageTypeValue`  INT           NOT NULL,
    `StoredAtUtc`       DATETIME(6)   NOT NULL,
    `LastVerifiedAtUtc` DATETIME(6)   NULL,
    `Metadata`          TEXT          NULL,
    INDEX `IX_DataLocations_EntityId` (`EntityId`),
    INDEX `IX_DataLocations_RegionCode` (`RegionCode`),
    INDEX `IX_DataLocations_DataCategory` (`DataCategory`)
);
