using System.Data;
using System.Text.Json;
using Dapper;
using Encina.Compliance.Consent;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.Consent;

/// <summary>
/// Dapper implementation of <see cref="IConsentVersionManager"/> for PostgreSQL.
/// Manages consent term versions and reconsent requirements.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses PostgreSQL-specific features:
/// <list type="bullet">
/// <item><description>Lowercase unquoted column identifiers (PostgreSQL folds to lowercase)</description></item>
/// <item><description>LIMIT for row restriction</description></item>
/// <item><description>TIMESTAMP for UTC datetime storage</description></item>
/// <item><description>Native BOOLEAN for boolean columns</description></item>
/// </list>
/// </para>
/// <para>
/// Supports GDPR Article 7 requirements by ensuring that consent is always linked
/// to the specific terms the data subject agreed to. When terms change materially,
/// existing consents can be flagged as requiring reconsent.
/// </para>
/// </remarks>
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
                SELECT versionid, purpose, effectivefromutc, description, requiresexplicitreconsent
                FROM {_versionsTableName}
                WHERE purpose = @Purpose
                ORDER BY effectivefromutc DESC
                LIMIT 1";

            var rows = await _connection.QueryAsync(sql, new { Purpose = purpose });
            var row = rows.FirstOrDefault();

            if (row is null)
            {
                return Left(EncinaErrors.Create(
                    code: "consent.version_not_found",
                    message: $"No consent version found for purpose '{purpose}'.",
                    details: new Dictionary<string, object?> { ["purpose"] = purpose }));
            }

            return Right(MapToConsentVersion(row));
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
                (versionid, purpose, effectivefromutc, description, requiresexplicitreconsent)
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
                    SET status = @RequiresReconsentStatus
                    WHERE purpose = @Purpose
                      AND status = @ActiveStatus
                      AND consentversionid != @NewVersionId";

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
            // Get the consented version id
            var consentSql = $@"
                SELECT consentversionid
                FROM {_consentTableName}
                WHERE subjectid = @SubjectId
                  AND purpose = @Purpose
                  AND status = @ActiveStatus";

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
                // No active consent found - reconsent is required
                return Right(true);
            }

            // Get the current version
            var versionSql = $@"
                SELECT versionid, purpose, effectivefromutc, description, requiresexplicitreconsent
                FROM {_versionsTableName}
                WHERE purpose = @Purpose
                ORDER BY effectivefromutc DESC
                LIMIT 1";

            var rows = await _connection.QueryAsync(versionSql, new { Purpose = purpose });
            var versionRow = rows.FirstOrDefault();

            if (versionRow is null)
            {
                // No version found - no reconsent needed
                return Right(false);
            }

            var currentVersionId = (string)versionRow.versionid;
            var requiresReconsent = (bool)versionRow.requiresexplicitreconsent;

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

    private static ConsentVersion MapToConsentVersion(dynamic row) => new()
    {
        VersionId = (string)row.versionid,
        Purpose = (string)row.purpose,
        EffectiveFromUtc = new DateTimeOffset((DateTime)row.effectivefromutc, TimeSpan.Zero),
        Description = (string)row.description,
        RequiresExplicitReconsent = (bool)row.requiresexplicitreconsent
    };
}
