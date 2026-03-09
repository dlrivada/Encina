-- =============================================
-- Create abac_policies table for SQL Server
-- ABAC standalone policy storage with JSON policy graph
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[abac_policies]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[abac_policies]
    (
        [Id] NVARCHAR(256) NOT NULL,
        [Version] NVARCHAR(256) NULL,
        [Description] NVARCHAR(MAX) NULL,
        [PolicyJson] NVARCHAR(MAX) NOT NULL,
        [IsEnabled] BIT NOT NULL DEFAULT 1,
        [Priority] INT NOT NULL DEFAULT 0,
        [CreatedAtUtc] DATETIME2(7) NOT NULL,
        [UpdatedAtUtc] DATETIME2(7) NOT NULL,

        CONSTRAINT [PK_abac_policies] PRIMARY KEY CLUSTERED ([Id]),

        INDEX [IX_abac_policies_IsEnabled_Priority] ([IsEnabled], [Priority])
    );
    PRINT 'Created table: abac_policies';
END
ELSE
BEGIN
    PRINT 'Table already exists: abac_policies';
END
GO
