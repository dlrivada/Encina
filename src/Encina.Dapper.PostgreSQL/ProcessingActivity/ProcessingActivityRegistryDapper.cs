using Dapper;
using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Diagnostics;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.ProcessingActivity;

/// <summary>
/// Dapper implementation of <see cref="IProcessingActivityRegistry"/> for PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates a new <see cref="NpgsqlConnection"/> per operation, making it
/// safe for singleton registration. Uses INSERT-only semantics for <see cref="RegisterActivityAsync"/>
/// (fails on duplicate <c>requesttypename</c>) and a separate UPDATE for <see cref="UpdateActivityAsync"/>.
/// </para>
/// <para>
/// Register via <c>AddEncinaProcessingActivityDapperPostgreSQL(connectionString)</c>.
/// </para>
/// </remarks>
public sealed class ProcessingActivityRegistryDapper : IProcessingActivityRegistry
{
    private const string SelectColumns = """
        id AS "Id", requesttypename AS "RequestTypeName", name AS "Name", purpose AS "Purpose",
        lawfulbasisvalue AS "LawfulBasisValue",
        categoriesofdatasubjectsjson AS "CategoriesOfDataSubjectsJson",
        categoriesofpersonaldatajson AS "CategoriesOfPersonalDataJson",
        recipientsjson AS "RecipientsJson",
        thirdcountrytransfers AS "ThirdCountryTransfers", safeguards AS "Safeguards",
        retentionperiodticks AS "RetentionPeriodTicks", securitymeasures AS "SecurityMeasures",
        createdatutc AS "CreatedAtUtc", lastupdatedatutc AS "LastUpdatedAtUtc"
        """;

    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessingActivityRegistryDapper"/> class.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
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

            const string sql = """
                INSERT INTO processingactivities
                    (id, requesttypename, name, purpose, lawfulbasisvalue,
                     categoriesofdatasubjectsjson, categoriesofpersonaldatajson, recipientsjson,
                     thirdcountrytransfers, safeguards, retentionperiodticks,
                     securitymeasures, createdatutc, lastupdatedatutc)
                VALUES
                    (@Id, @RequestTypeName, @Name, @Purpose, @LawfulBasisValue,
                     @CategoriesOfDataSubjectsJson, @CategoriesOfPersonalDataJson, @RecipientsJson,
                     @ThirdCountryTransfers, @Safeguards, @RetentionPeriodTicks,
                     @SecurityMeasures, @CreatedAtUtc, @LastUpdatedAtUtc)
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await connection.ExecuteAsync(sql, new
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
                entity.CreatedAtUtc,
                entity.LastUpdatedAtUtc
            }).ConfigureAwait(false);

            ProcessingActivityDiagnostics.RecordSuccess(trace, "register");
            return Right(Unit.Default);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
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
            var sql = $"""
                SELECT {SelectColumns}
                FROM processingactivities
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var entities = await connection.QueryAsync<ProcessingActivityEntity>(sql).ConfigureAwait(false);

            var results = new List<Compliance.GDPR.ProcessingActivity>();
            foreach (var entity in entities)
            {
                var domain = ProcessingActivityMapper.ToDomain(entity);
                if (domain is not null)
                {
                    results.Add(domain);
                }
            }

            ProcessingActivityDiagnostics.RecordSuccess(trace, results.Count, "get_all");
            return Right<EncinaError, IReadOnlyList<Compliance.GDPR.ProcessingActivity>>(results);
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
            var sql = $"""
                SELECT {SelectColumns}
                FROM processingactivities
                WHERE requesttypename = @RequestTypeName
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var entity = await connection.QueryFirstOrDefaultAsync<ProcessingActivityEntity>(
                sql, new { RequestTypeName = requestType.AssemblyQualifiedName! }).ConfigureAwait(false);

            if (entity is not null)
            {
                var domain = ProcessingActivityMapper.ToDomain(entity);
                ProcessingActivityDiagnostics.RecordSuccess(trace, "get_by_request_type");
                return domain is not null
                    ? Right<EncinaError, Option<Compliance.GDPR.ProcessingActivity>>(Some(domain))
                    : Right<EncinaError, Option<Compliance.GDPR.ProcessingActivity>>(None);
            }

            ProcessingActivityDiagnostics.RecordSuccess(trace, "get_by_request_type");
            return Right<EncinaError, Option<Compliance.GDPR.ProcessingActivity>>(None);
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

            const string sql = """
                UPDATE processingactivities
                SET name = @Name,
                    purpose = @Purpose,
                    lawfulbasisvalue = @LawfulBasisValue,
                    categoriesofdatasubjectsjson = @CategoriesOfDataSubjectsJson,
                    categoriesofpersonaldatajson = @CategoriesOfPersonalDataJson,
                    recipientsjson = @RecipientsJson,
                    thirdcountrytransfers = @ThirdCountryTransfers,
                    safeguards = @Safeguards,
                    retentionperiodticks = @RetentionPeriodTicks,
                    securitymeasures = @SecurityMeasures,
                    lastupdatedatutc = @LastUpdatedAtUtc
                WHERE requesttypename = @RequestTypeName
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var rowsAffected = await connection.ExecuteAsync(sql, new
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
                entity.LastUpdatedAtUtc,
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
}
