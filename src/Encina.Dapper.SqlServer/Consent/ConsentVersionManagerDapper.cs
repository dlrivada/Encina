using System.Data;
using System.Text.Json;
using Dapper;
using Encina.Compliance.Consent;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.Consent;

/// <summary>
/// Dapper implementation of <see cref="IConsentVersionManager"/> for SQL Server.
/// Manages consent term versions and reconsent requirements.
/// </summary>
public sealed class ConsentVersionManagerDapper : IConsentVersionManager
{
    private readonly IDbConnection _connection;
    private readonly string _versionsTableName;
    private readonly string _consentTableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentVersionManagerDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="versionsTableName">The versions table name (default: ConsentVersions).</param>
    /// <param name="consentTableName">The consent records table name (default: ConsentRecords).</param>
    public ConsentVersionManagerDapper(
        IDbConnection connection,
        string versionsTableName = "ConsentVersions",
        string consentTableName = "ConsentRecords")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _versionsTableName = SqlIdentifierValidator.ValidateTableName(versionsTableName);
        _consentTableName = SqlIdentifierValidator.ValidateTableName(consentTableName);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, ConsentVersion>> GetCurrentVersionAsync(
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            var sql = $@"
                SELECT TOP (1) VersionId, Purpose, EffectiveFromUtc, Description, RequiresExplicitReconsent
                FROM {_versionsTableName}
                WHERE Purpose = @Purpose
                ORDER BY EffectiveFromUtc DESC";

            var rows = await _connection.QueryAsync(sql, new { Purpose = purpose });
            var row = rows.FirstOrDefault();

            if (row is null)
            {
                return Left(EncinaErrors.Create(
                    code: "consent.version_not_found",
                    message: $"No consent version found for purpose '{purpose}'.",
                    details: new Dictionary<string, object?> { ["purpose"] = purpose }));
            }

            return Right(new ConsentVersion
            {
                VersionId = (string)row.VersionId,
                Purpose = (string)row.Purpose,
                EffectiveFromUtc = new DateTimeOffset((DateTime)row.EffectiveFromUtc, TimeSpan.Zero),
                Description = (string)row.Description,
                RequiresExplicitReconsent = (bool)row.RequiresExplicitReconsent
            });
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.version_store_error",
                message: $"Failed to get current version: {ex.Message}",
                details: new Dictionary<string, object?> { ["purpose"] = purpose }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> PublishNewVersionAsync(
        ConsentVersion version,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        try
        {
            var insertSql = $@"
                INSERT INTO {_versionsTableName}
                (VersionId, Purpose, EffectiveFromUtc, Description, RequiresExplicitReconsent)
                VALUES
                (@VersionId, @Purpose, @EffectiveFromUtc, @Description, @RequiresExplicitReconsent)";

            await _connection.ExecuteAsync(insertSql, new
            {
                version.VersionId,
                version.Purpose,
                EffectiveFromUtc = version.EffectiveFromUtc.UtcDateTime,
                version.Description,
                version.RequiresExplicitReconsent
            });

            // If the new version requires explicit reconsent, update existing active consents
            if (version.RequiresExplicitReconsent)
            {
                var updateSql = $@"
                    UPDATE {_consentTableName}
                    SET Status = @RequiresReconsentStatus
                    WHERE Purpose = @Purpose
                      AND Status = @ActiveStatus
                      AND ConsentVersionId != @NewVersionId";

                await _connection.ExecuteAsync(updateSql, new
                {
                    RequiresReconsentStatus = (int)ConsentStatus.RequiresReconsent,
                    version.Purpose,
                    ActiveStatus = (int)ConsentStatus.Active,
                    NewVersionId = version.VersionId
                });
            }

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.version_store_error",
                message: $"Failed to publish new version: {ex.Message}",
                details: new Dictionary<string, object?> { ["versionId"] = version.VersionId, ["purpose"] = version.Purpose }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> RequiresReconsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            // Get the consent record's version
            var consentSql = $@"
                SELECT ConsentVersionId
                FROM {_consentTableName}
                WHERE SubjectId = @SubjectId
                  AND Purpose = @Purpose
                  AND Status = @ActiveStatus";

            var consentedVersionId = await _connection.QueryFirstOrDefaultAsync<string?>(
                consentSql,
                new
                {
                    SubjectId = subjectId,
                    Purpose = purpose,
                    ActiveStatus = (int)ConsentStatus.Active
                });

            if (consentedVersionId is null)
            {
                // No active consent found -- reconsent is required
                return Right(true);
            }

            // Get the current version
            var versionSql = $@"
                SELECT TOP (1) VersionId, RequiresExplicitReconsent
                FROM {_versionsTableName}
                WHERE Purpose = @Purpose
                ORDER BY EffectiveFromUtc DESC";

            var rows = await _connection.QueryAsync(versionSql, new { Purpose = purpose });
            var row = rows.FirstOrDefault();

            if (row is null)
            {
                // No version found -- no reconsent needed
                return Right(false);
            }

            var currentVersionId = (string)row.VersionId;
            var requiresReconsent = (bool)row.RequiresExplicitReconsent;

            // Reconsent needed if version differs and new version requires it
            return Right(currentVersionId != consentedVersionId && requiresReconsent);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.version_store_error",
                message: $"Failed to check reconsent requirement: {ex.Message}",
                details: new Dictionary<string, object?> { ["subjectId"] = subjectId, ["purpose"] = purpose }));
        }
    }
}
