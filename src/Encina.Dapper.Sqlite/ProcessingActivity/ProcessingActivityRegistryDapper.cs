using System.Globalization;
using Dapper;
using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Diagnostics;
using LanguageExt;
using Microsoft.Data.Sqlite;
using static LanguageExt.Prelude;

namespace Encina.Dapper.Sqlite.ProcessingActivity;

/// <summary>
/// Dapper implementation of <see cref="IProcessingActivityRegistry"/> for SQLite.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates a new <see cref="SqliteConnection"/> per operation, making it
/// safe for singleton registration. Uses INSERT-only semantics for <see cref="RegisterActivityAsync"/>
/// (fails on duplicate <c>RequestTypeName</c>) and a separate UPDATE for <see cref="UpdateActivityAsync"/>.
/// </para>
/// <para>
/// DateTimeOffset values are stored as ISO 8601 strings (round-trip "O" format)
/// because SQLite does not have a native DateTimeOffset type.
/// </para>
/// <para>
/// Register via <c>AddEncinaProcessingActivityDapperSqlite(connectionString)</c>.
/// </para>
/// </remarks>
public sealed class ProcessingActivityRegistryDapper : IProcessingActivityRegistry
{
    private const string TableName = "ProcessingActivities";

    private const string InsertSql = $"""
        INSERT INTO {TableName}
            (Id, RequestTypeName, Name, Purpose, LawfulBasisValue,
             CategoriesOfDataSubjectsJson, CategoriesOfPersonalDataJson, RecipientsJson,
             ThirdCountryTransfers, Safeguards, RetentionPeriodTicks,
             SecurityMeasures, CreatedAtUtc, LastUpdatedAtUtc)
        VALUES
            (@Id, @RequestTypeName, @Name, @Purpose, @LawfulBasisValue,
             @CategoriesOfDataSubjectsJson, @CategoriesOfPersonalDataJson, @RecipientsJson,
             @ThirdCountryTransfers, @Safeguards, @RetentionPeriodTicks,
             @SecurityMeasures, @CreatedAtUtc, @LastUpdatedAtUtc)
        """;

    private const string UpdateSql = $"""
        UPDATE {TableName}
        SET Name = @Name,
            Purpose = @Purpose,
            LawfulBasisValue = @LawfulBasisValue,
            CategoriesOfDataSubjectsJson = @CategoriesOfDataSubjectsJson,
            CategoriesOfPersonalDataJson = @CategoriesOfPersonalDataJson,
            RecipientsJson = @RecipientsJson,
            ThirdCountryTransfers = @ThirdCountryTransfers,
            Safeguards = @Safeguards,
            RetentionPeriodTicks = @RetentionPeriodTicks,
            SecurityMeasures = @SecurityMeasures,
            LastUpdatedAtUtc = @LastUpdatedAtUtc
        WHERE RequestTypeName = @RequestTypeName
        """;

    private const string SelectByRequestTypeNameSql = $"""
        SELECT Id, RequestTypeName, Name, Purpose, LawfulBasisValue,
               CategoriesOfDataSubjectsJson, CategoriesOfPersonalDataJson, RecipientsJson,
               ThirdCountryTransfers, Safeguards, RetentionPeriodTicks,
               SecurityMeasures, CreatedAtUtc, LastUpdatedAtUtc
        FROM {TableName}
        WHERE RequestTypeName = @RequestTypeName
        """;

    private const string SelectAllSql = $"""
        SELECT Id, RequestTypeName, Name, Purpose, LawfulBasisValue,
               CategoriesOfDataSubjectsJson, CategoriesOfPersonalDataJson, RecipientsJson,
               ThirdCountryTransfers, Safeguards, RetentionPeriodTicks,
               SecurityMeasures, CreatedAtUtc, LastUpdatedAtUtc
        FROM {TableName}
        """;

    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessingActivityRegistryDapper"/> class.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string.</param>
    public ProcessingActivityRegistryDapper(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RegisterActivityAsync(
        Compliance.GDPR.ProcessingActivity activity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);

        using var trace = ProcessingActivityDiagnostics.StartRegistration(activity.RequestType);

        try
        {
            var entity = ProcessingActivityMapper.ToEntity(activity);

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await connection.ExecuteAsync(InsertSql, new
            {
                entity.Id,
                entity.RequestTypeName,
                entity.Name,
                entity.Purpose,
                entity.LawfulBasisValue,
                entity.CategoriesOfDataSubjectsJson,
                entity.CategoriesOfPersonalDataJson,
                entity.RecipientsJson,
                entity.ThirdCountryTransfers,
                entity.Safeguards,
                entity.RetentionPeriodTicks,
                entity.SecurityMeasures,
                CreatedAtUtc = entity.CreatedAtUtc.ToString("O"),
                LastUpdatedAtUtc = entity.LastUpdatedAtUtc.ToString("O")
            }).ConfigureAwait(false);

            ProcessingActivityDiagnostics.RecordSuccess(trace, "register");
            return Right(Unit.Default);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            ProcessingActivityDiagnostics.RecordFailure(trace, "register", "duplicate");
            return Left(GDPRErrors.ProcessingActivityDuplicate(activity.RequestType.AssemblyQualifiedName!));
        }
        catch (Exception ex)
        {
            ProcessingActivityDiagnostics.RecordFailure(trace, "register", ex.Message);
            return Left(GDPRErrors.ProcessingActivityStoreError("RegisterActivity", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<Compliance.GDPR.ProcessingActivity>>> GetAllActivitiesAsync(
        CancellationToken cancellationToken = default)
    {
        using var trace = ProcessingActivityDiagnostics.StartGetAll();

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var rows = await connection.QueryAsync(SelectAllSql).ConfigureAwait(false);

            var results = new List<Compliance.GDPR.ProcessingActivity>();
            foreach (var row in rows)
            {
                var entity = MapToEntity(row);
                var domain = ProcessingActivityMapper.ToDomain(entity);
                if (domain is not null)
                {
                    results.Add(domain);
                }
            }

            ProcessingActivityDiagnostics.RecordSuccess(trace, results.Count, "get_all");
            return Right<EncinaError, IReadOnlyList<Compliance.GDPR.ProcessingActivity>>(results.AsReadOnly());
        }
        catch (Exception ex)
        {
            ProcessingActivityDiagnostics.RecordFailure(trace, "get_all", ex.Message);
            return Left(GDPRErrors.ProcessingActivityStoreError("GetAllActivities", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<Compliance.GDPR.ProcessingActivity>>> GetActivityByRequestTypeAsync(
        Type requestType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        using var trace = ProcessingActivityDiagnostics.StartGetByRequestType(requestType);

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var rows = await connection.QueryAsync(
                SelectByRequestTypeNameSql,
                new { RequestTypeName = requestType.AssemblyQualifiedName! }).ConfigureAwait(false);

            var row = rows.FirstOrDefault();
            if (row is null)
            {
                ProcessingActivityDiagnostics.RecordSuccess(trace, "get_by_request_type");
                return Right<EncinaError, Option<Compliance.GDPR.ProcessingActivity>>(None);
            }

            var entity = MapToEntity(row);
            var domain = ProcessingActivityMapper.ToDomain(entity);

            ProcessingActivityDiagnostics.RecordSuccess(trace, "get_by_request_type");
            return domain is not null
                ? Right<EncinaError, Option<Compliance.GDPR.ProcessingActivity>>(Some(domain))
                : Right<EncinaError, Option<Compliance.GDPR.ProcessingActivity>>(None);
        }
        catch (Exception ex)
        {
            ProcessingActivityDiagnostics.RecordFailure(trace, "get_by_request_type", ex.Message);
            return Left(GDPRErrors.ProcessingActivityStoreError("GetActivityByRequestType", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdateActivityAsync(
        Compliance.GDPR.ProcessingActivity activity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);

        using var trace = ProcessingActivityDiagnostics.StartUpdate(activity.RequestType);

        try
        {
            var entity = ProcessingActivityMapper.ToEntity(activity);

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var rowsAffected = await connection.ExecuteAsync(UpdateSql, new
            {
                entity.Name,
                entity.Purpose,
                entity.LawfulBasisValue,
                entity.CategoriesOfDataSubjectsJson,
                entity.CategoriesOfPersonalDataJson,
                entity.RecipientsJson,
                entity.ThirdCountryTransfers,
                entity.Safeguards,
                entity.RetentionPeriodTicks,
                entity.SecurityMeasures,
                LastUpdatedAtUtc = entity.LastUpdatedAtUtc.ToString("O"),
                entity.RequestTypeName
            }).ConfigureAwait(false);

            if (rowsAffected > 0)
            {
                ProcessingActivityDiagnostics.RecordSuccess(trace, "update");
                return Right(Unit.Default);
            }

            ProcessingActivityDiagnostics.RecordFailure(trace, "update", "not_found");
            return Left(GDPRErrors.ProcessingActivityNotFound(activity.RequestType.AssemblyQualifiedName!));
        }
        catch (Exception ex)
        {
            ProcessingActivityDiagnostics.RecordFailure(trace, "update", ex.Message);
            return Left(GDPRErrors.ProcessingActivityStoreError("UpdateActivity", ex.Message));
        }
    }

    private static ProcessingActivityEntity MapToEntity(dynamic row) => new()
    {
        Id = (string)row.Id,
        RequestTypeName = (string)row.RequestTypeName,
        Name = (string)row.Name,
        Purpose = (string)row.Purpose,
        LawfulBasisValue = (int)(long)row.LawfulBasisValue,
        CategoriesOfDataSubjectsJson = (string)row.CategoriesOfDataSubjectsJson,
        CategoriesOfPersonalDataJson = (string)row.CategoriesOfPersonalDataJson,
        RecipientsJson = (string)row.RecipientsJson,
        ThirdCountryTransfers = row.ThirdCountryTransfers is null or DBNull ? null : (string)row.ThirdCountryTransfers,
        Safeguards = row.Safeguards is null or DBNull ? null : (string)row.Safeguards,
        RetentionPeriodTicks = (long)row.RetentionPeriodTicks,
        SecurityMeasures = (string)row.SecurityMeasures,
        CreatedAtUtc = DateTimeOffset.Parse((string)row.CreatedAtUtc, null, DateTimeStyles.RoundtripKind),
        LastUpdatedAtUtc = DateTimeOffset.Parse((string)row.LastUpdatedAtUtc, null, DateTimeStyles.RoundtripKind)
    };
}
