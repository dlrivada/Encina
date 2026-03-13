CREATE TABLE [dbo].[Processors] (
    [Id]                                NVARCHAR(256)  NOT NULL PRIMARY KEY,
    [Name]                              NVARCHAR(450)  NOT NULL,
    [Country]                           NVARCHAR(100)  NOT NULL,
    [ContactEmail]                      NVARCHAR(256)  NULL,
    [ParentProcessorId]                 NVARCHAR(256)  NULL,
    [Depth]                             INT            NOT NULL,
    [SubProcessorAuthorizationTypeValue] INT           NOT NULL,
    [TenantId]                          NVARCHAR(256)  NULL,
    [ModuleId]                          NVARCHAR(256)  NULL,
    [CreatedAtUtc]                      DATETIME2(7)   NOT NULL,
    [LastUpdatedAtUtc]                  DATETIME2(7)   NOT NULL,
    INDEX [IX_Processors_ParentProcessorId] ([ParentProcessorId]),
    INDEX [IX_Processors_TenantId] ([TenantId])
);
GO
