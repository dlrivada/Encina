-- =============================================
-- Create ConsentVersions table
-- For consent version tracking and reconsent management
-- =============================================

CREATE TABLE [dbo].[ConsentVersions]
(
    [VersionId] NVARCHAR(256) NOT NULL PRIMARY KEY,
    [Purpose] NVARCHAR(256) NOT NULL,
    [EffectiveFromUtc] DATETIME2(7) NOT NULL,
    [Description] NVARCHAR(MAX) NOT NULL,
    [RequiresExplicitReconsent] BIT NOT NULL DEFAULT 0,

    INDEX [IX_ConsentVersions_Purpose] ([Purpose]),
    INDEX [IX_ConsentVersions_Purpose_EffectiveFromUtc] ([Purpose], [EffectiveFromUtc])
);
GO
