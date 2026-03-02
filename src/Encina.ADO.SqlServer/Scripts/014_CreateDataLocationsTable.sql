CREATE TABLE [dbo].[DataLocations] (
    [Id]                NVARCHAR(36)    NOT NULL PRIMARY KEY,
    [EntityId]          NVARCHAR(256)   NOT NULL,
    [DataCategory]      NVARCHAR(256)   NOT NULL,
    [RegionCode]        NVARCHAR(32)    NOT NULL,
    [StorageTypeValue]  INT             NOT NULL,
    [StoredAtUtc]       DATETIME2(7)    NOT NULL,
    [LastVerifiedAtUtc] DATETIME2(7)    NULL,
    [Metadata]          NVARCHAR(MAX)   NULL,
    INDEX [IX_DataLocations_EntityId] ([EntityId]),
    INDEX [IX_DataLocations_RegionCode] ([RegionCode]),
    INDEX [IX_DataLocations_DataCategory] ([DataCategory])
);
GO
