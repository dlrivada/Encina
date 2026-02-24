using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Diagnostics;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.ADO.PostgreSQL.ProcessingActivity;

/// <summary>
/// ADO.NET implementation of <see cref="IProcessingActivityRegistry"/> for PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates a new <see cref="NpgsqlConnection"/> per operation, making it
/// safe for singleton registration. Uses INSERT-only semantics for <see cref="RegisterActivityAsync"/>
/// (fails on duplicate <c>requesttypename</c>) and a separate UPDATE for <see cref="UpdateActivityAsync"/>.
/// </para>
/// <para>
/// Register via <c>AddEncinaProcessingActivityADOPostgreSQL(connectionString)</c>.
/// </para>
/// </remarks>
public sealed class ProcessingActivityRegistryADO : IProcessingActivityRegistry
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessingActivityRegistryADO"/> class.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    public ProcessingActivityRegistryADO(string connectionString)
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
                    (@id, @requesttypename, @name, @purpose, @lawfulbasisvalue,
                     @categoriesofdatasubjectsjson, @categoriesofpersonaldatajson, @recipientsjson,
                     @thirdcountrytransfers, @safeguards, @retentionperiodticks,
                     @securitymeasures, @createdatutc, @lastupdatedatutc)
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddActivityParameters(command, entity);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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
            const string sql = """
                SELECT id, requesttypename, name, purpose, lawfulbasisvalue,
                       categoriesofdatasubjectsjson, categoriesofpersonaldatajson, recipientsjson,
                       thirdcountrytransfers, safeguards, retentionperiodticks,
                       securitymeasures, createdatutc, lastupdatedatutc
                FROM processingactivities
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            var results = new List<Compliance.GDPR.ProcessingActivity>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var entity = MapToEntity(reader);
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
            const string sql = """
                SELECT id, requesttypename, name, purpose, lawfulbasisvalue,
                       categoriesofdatasubjectsjson, categoriesofpersonaldatajson, recipientsjson,
                       thirdcountrytransfers, safeguards, retentionperiodticks,
                       securitymeasures, createdatutc, lastupdatedatutc
                FROM processingactivities
                WHERE requesttypename = @requesttypename
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@requesttypename", requestType.AssemblyQualifiedName!);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var entity = MapToEntity(reader);
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
                SET name = @name,
                    purpose = @purpose,
                    lawfulbasisvalue = @lawfulbasisvalue,
                    categoriesofdatasubjectsjson = @categoriesofdatasubjectsjson,
                    categoriesofpersonaldatajson = @categoriesofpersonaldatajson,
                    recipientsjson = @recipientsjson,
                    thirdcountrytransfers = @thirdcountrytransfers,
                    safeguards = @safeguards,
                    retentionperiodticks = @retentionperiodticks,
                    securitymeasures = @securitymeasures,
                    lastupdatedatutc = @lastupdatedatutc
                WHERE requesttypename = @requesttypename
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddActivityParameters(command, entity);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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

    private static void AddActivityParameters(NpgsqlCommand command, ProcessingActivityEntity entity)
    {
        command.Parameters.AddWithValue("@id", entity.Id);
        command.Parameters.AddWithValue("@requesttypename", entity.RequestTypeName);
        command.Parameters.AddWithValue("@name", entity.Name);
        command.Parameters.AddWithValue("@purpose", entity.Purpose);
        command.Parameters.AddWithValue("@lawfulbasisvalue", entity.LawfulBasisValue);
        command.Parameters.AddWithValue("@categoriesofdatasubjectsjson", entity.CategoriesOfDataSubjectsJson);
        command.Parameters.AddWithValue("@categoriesofpersonaldatajson", entity.CategoriesOfPersonalDataJson);
        command.Parameters.AddWithValue("@recipientsjson", entity.RecipientsJson);
        command.Parameters.AddWithValue("@thirdcountrytransfers", (object?)entity.ThirdCountryTransfers ?? DBNull.Value);
        command.Parameters.AddWithValue("@safeguards", (object?)entity.Safeguards ?? DBNull.Value);
        command.Parameters.AddWithValue("@retentionperiodticks", entity.RetentionPeriodTicks);
        command.Parameters.AddWithValue("@securitymeasures", entity.SecurityMeasures);
        command.Parameters.AddWithValue("@createdatutc", entity.CreatedAtUtc);
        command.Parameters.AddWithValue("@lastupdatedatutc", entity.LastUpdatedAtUtc);
    }

    private static ProcessingActivityEntity MapToEntity(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetString(reader.GetOrdinal("id")),
        RequestTypeName = reader.GetString(reader.GetOrdinal("requesttypename")),
        Name = reader.GetString(reader.GetOrdinal("name")),
        Purpose = reader.GetString(reader.GetOrdinal("purpose")),
        LawfulBasisValue = reader.GetInt32(reader.GetOrdinal("lawfulbasisvalue")),
        CategoriesOfDataSubjectsJson = reader.GetString(reader.GetOrdinal("categoriesofdatasubjectsjson")),
        CategoriesOfPersonalDataJson = reader.GetString(reader.GetOrdinal("categoriesofpersonaldatajson")),
        RecipientsJson = reader.GetString(reader.GetOrdinal("recipientsjson")),
        ThirdCountryTransfers = reader.IsDBNull(reader.GetOrdinal("thirdcountrytransfers")) ? null : reader.GetString(reader.GetOrdinal("thirdcountrytransfers")),
        Safeguards = reader.IsDBNull(reader.GetOrdinal("safeguards")) ? null : reader.GetString(reader.GetOrdinal("safeguards")),
        RetentionPeriodTicks = reader.GetInt64(reader.GetOrdinal("retentionperiodticks")),
        SecurityMeasures = reader.GetString(reader.GetOrdinal("securitymeasures")),
        CreatedAtUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("createdatutc")),
        LastUpdatedAtUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("lastupdatedatutc"))
    };
}
