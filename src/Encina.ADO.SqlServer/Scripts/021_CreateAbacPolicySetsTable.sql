-- =============================================
-- Create abac_policy_sets table for SQL Server
-- ABAC policy set storage with JSON policy graph
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[abac_policy_sets]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[abac_policy_sets]
    (
        [Id] NVARCHAR(256) NOT NULL,
        [Version] NVARCHAR(256) NULL,
        [Description] NVARCHAR(MAX) NULL,
        [PolicyJson] NVARCHAR(MAX) NOT NULL,
        [IsEnabled] BIT NOT NULL DEFAULT 1,
        [Priority] INT NOT NULL DEFAULT 0,
        [CreatedAtUtc] DATETIME2(7) NOT NULL,
        [UpdatedAtUtc] DATETIME2(7) NOT NULL,

        CONSTRAINT [PK_abac_policy_sets] PRIMARY KEY CLUSTERED ([Id]),

        INDEX [IX_abac_policy_sets_IsEnabled_Priority] ([IsEnabled], [Priority])
    );
    PRINT 'Created table: abac_policy_sets';
END
ELSE
BEGIN
    PRINT 'Table already exists: abac_policy_sets';
END
GO
