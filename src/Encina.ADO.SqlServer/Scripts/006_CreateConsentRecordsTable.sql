-- =============================================
-- Create ConsentRecords table
-- For GDPR consent record lifecycle management
-- =============================================

CREATE TABLE [dbo].[ConsentRecords]
(
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [SubjectId] NVARCHAR(256) NOT NULL,
    [Purpose] NVARCHAR(256) NOT NULL,
    [Status] INT NOT NULL,
    [ConsentVersionId] NVARCHAR(256) NOT NULL,
    [GivenAtUtc] DATETIME2(7) NOT NULL,
    [WithdrawnAtUtc] DATETIME2(7) NULL,
    [ExpiresAtUtc] DATETIME2(7) NULL,
    [Source] NVARCHAR(256) NOT NULL,
    [IpAddress] NVARCHAR(45) NULL,
    [ProofOfConsent] NVARCHAR(MAX) NULL,
    [Metadata] NVARCHAR(MAX) NOT NULL DEFAULT '{}',

    INDEX [IX_ConsentRecords_SubjectId] ([SubjectId]),
    INDEX [IX_ConsentRecords_SubjectId_Purpose] ([SubjectId], [Purpose]),
    INDEX [IX_ConsentRecords_Status] ([Status])
);
GO
