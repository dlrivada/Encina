CREATE TABLE [dbo].[ResidencyPolicies] (
    [DataCategory]              NVARCHAR(256)   NOT NULL PRIMARY KEY,
    [AllowedRegionCodes]        NVARCHAR(1024)  NOT NULL,
    [RequireAdequacyDecision]   BIT             NOT NULL,
    [AllowedTransferBasesValue] NVARCHAR(256)   NULL,
    [CreatedAtUtc]              DATETIME2(7)    NOT NULL,
    [LastModifiedAtUtc]         DATETIME2(7)    NULL
);
GO
