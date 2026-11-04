-- =============================================
-- BreakTheGlassElevations - Break-the-glass emergency access
-- One row per justified, time-boxed elevation of a user's access to one data subject's data in one scope.
-- Lengths come from Encina.Security.BreakTheGlass.BreakTheGlassLimits.
-- Status: 0 Active, 1 Expired, 2 Revoked. ReviewOutcome: 0 Justified, 1 Unjustified (NULL while unreviewed).
-- =============================================
-- Key columns use a binary collation so that they compare case-sensitively, like PostgreSQL and MongoDB.
-- ActiveKey is the atomic arbiter of "one active elevation per tenant, user, subject and scope": a hash of the key parts joined
-- with NCHAR(31) (so ("a","bc") and ("ab","c") differ), set only while the row is Active. Any other row carries its own ElevationId
-- there, so it is unique and never collides: SQL Server rejects a filtered index over a computed column, and a plain unique index
-- would admit a single NULL. A concurrent second insert fails with error 2627/2601.
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BreakTheGlassElevations]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[BreakTheGlassElevations] (
        [ElevationId]         UNIQUEIDENTIFIER NOT NULL,
        [TenantId]            NVARCHAR(128)    COLLATE Latin1_General_100_BIN2 NULL,
        [UserId]              NVARCHAR(256)    COLLATE Latin1_General_100_BIN2 NOT NULL,
        [SubjectId]           NVARCHAR(256)    COLLATE Latin1_General_100_BIN2 NOT NULL,
        [Scope]               NVARCHAR(256)    COLLATE Latin1_General_100_BIN2 NOT NULL,
        [Justification]       NVARCHAR(2000)   NOT NULL,
        [OpenedAtUtc]         DATETIME2(7)     NOT NULL,
        [ExpiresAtUtc]        DATETIME2(7)     NOT NULL,
        [Status]              INT              NOT NULL,
        [ClosedAtUtc]         DATETIME2(7)     NULL,
        [CloseReason]         NVARCHAR(256)    COLLATE Latin1_General_100_BIN2 NULL,
        [ExpiryRecordedAtUtc] DATETIME2(7)     NULL,
        [ReviewOutcome]       INT              NULL,
        [ReviewedByUserId]    NVARCHAR(256)    COLLATE Latin1_General_100_BIN2 NULL,
        [ReviewedAtUtc]       DATETIME2(7)     NULL,
        [ReviewNote]          NVARCHAR(2000)   NULL,
        [ActiveKey] AS (CASE WHEN [Status] = 0
            THEN CAST(HASHBYTES('SHA2_256', CONCAT(COALESCE([TenantId], N''), NCHAR(31), [UserId], NCHAR(31), [SubjectId], NCHAR(31), [Scope])) AS BINARY(32))
            ELSE CAST([ElevationId] AS BINARY(32)) END) PERSISTED,

        CONSTRAINT [PK_BreakTheGlassElevations] PRIMARY KEY CLUSTERED ([ElevationId])
    );

    CREATE UNIQUE NONCLUSTERED INDEX [UX_BreakTheGlassElevations_ActiveKey]
        ON [dbo].[BreakTheGlassElevations] ([ActiveKey]);
    CREATE NONCLUSTERED INDEX [IX_BreakTheGlassElevations_ReviewQueue]
        ON [dbo].[BreakTheGlassElevations] ([TenantId], [ReviewOutcome], [OpenedAtUtc]);
    CREATE NONCLUSTERED INDEX [IX_BreakTheGlassElevations_Expiry]
        ON [dbo].[BreakTheGlassElevations] ([Status], [ExpiresAtUtc]);
    PRINT 'Created table: BreakTheGlassElevations';
END
ELSE
BEGIN
    PRINT 'Table already exists: BreakTheGlassElevations';
END
GO
