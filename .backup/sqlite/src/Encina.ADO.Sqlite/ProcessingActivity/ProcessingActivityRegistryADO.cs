using System.Globalization;
using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Diagnostics;
using LanguageExt;
using Microsoft.Data.Sqlite;
using static LanguageExt.Prelude;

namespace Encina.ADO.Sqlite.ProcessingActivity;

/// <summary>
/// ADO.NET implementation of <see cref="IProcessingActivityRegistry"/> for SQLite.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates a new <see cref="SqliteConnection"/> per operation, making it
/// safe for singleton registration. Uses INSERT-only semantics for <see cref="RegisterActivityAsync"/>
/// (fails on duplicate <c>RequestTypeName</c>) and a separate UPDATE for <see cref="UpdateActivityAsync"/>.
/// </para>
/// <para>
/// DateTimeOffset values are stored as ISO 8601 strings (round-trip "O" format) because SQLite
/// does not have a native DateTimeOffset type.
/// </para>
/// <para>
/// Register via <c>AddEncinaProcessingActivityADOSqlite(connectionString)</c>.
/// </para>
/// </remarks>
public sealed class ProcessingActivityRegistryADO : IProcessingActivityRegistry
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessingActivityRegistryADO"/> class.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string.</param>
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
                INSERT INTO ProcessingActivities
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

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddActivityParameters(command, entity);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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
            const string sql = """
                SELECT Id, RequestTypeName, Name, Purpose, LawfulBasisValue,
                       CategoriesOfDataSubjectsJson, CategoriesOfPersonalDataJson, RecipientsJson,
                       ThirdCountryTransfers, Safeguards, RetentionPeriodTicks,
                       SecurityMeasures, CreatedAtUtc, LastUpdatedAtUtc
                FROM ProcessingActivities
                """;

            await using var connection = new SqliteConnection(_connectionString);
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
                SELECT Id, RequestTypeName, Name, Purpose, LawfulBasisValue,
                       CategoriesOfDataSubjectsJson, CategoriesOfPersonalDataJson, RecipientsJson,
                       ThirdCountryTransfers, Safeguards, RetentionPeriodTicks,
                       SecurityMeasures, CreatedAtUtc, LastUpdatedAtUtc
                FROM ProcessingActivities
                WHERE RequestTypeName = @RequestTypeName
                """;

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@RequestTypeName", requestType.AssemblyQualifiedName!);

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
                UPDATE ProcessingActivities
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

            await using var connection = new SqliteConnection(_connectionString);
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

    private static void AddActivityParameters(SqliteCommand command, ProcessingActivityEntity entity)
    {
        command.Parameters.AddWithValue("@Id", entity.Id);
        command.Parameters.AddWithValue("@RequestTypeName", entity.RequestTypeName);
        command.Parameters.AddWithValue("@Name", entity.Name);
        command.Parameters.AddWithValue("@Purpose", entity.Purpose);
        command.Parameters.AddWithValue("@LawfulBasisValue", entity.LawfulBasisValue);
        command.Parameters.AddWithValue("@CategoriesOfDataSubjectsJson", entity.CategoriesOfDataSubjectsJson);
        command.Parameters.AddWithValue("@CategoriesOfPersonalDataJson", entity.CategoriesOfPersonalDataJson);
        command.Parameters.AddWithValue("@RecipientsJson", entity.RecipientsJson);
        command.Parameters.AddWithValue("@ThirdCountryTransfers", (object?)entity.ThirdCountryTransfers ?? DBNull.Value);
        command.Parameters.AddWithValue("@Safeguards", (object?)entity.Safeguards ?? DBNull.Value);
        command.Parameters.AddWithValue("@RetentionPeriodTicks", entity.RetentionPeriodTicks);
        command.Parameters.AddWithValue("@SecurityMeasures", entity.SecurityMeasures);
        command.Parameters.AddWithValue("@CreatedAtUtc", entity.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@LastUpdatedAtUtc", entity.LastUpdatedAtUtc.ToString("O"));
    }

    private static ProcessingActivityEntity MapToEntity(SqliteDataReader reader) => new()
    {
        Id = reader.GetString(reader.GetOrdinal("Id")),
        RequestTypeName = reader.GetString(reader.GetOrdinal("RequestTypeName")),
        Name = reader.GetString(reader.GetOrdinal("Name")),
        Purpose = reader.GetString(reader.GetOrdinal("Purpose")),
        LawfulBasisValue = reader.GetInt32(reader.GetOrdinal("LawfulBasisValue")),
        CategoriesOfDataSubjectsJson = reader.GetString(reader.GetOrdinal("CategoriesOfDataSubjectsJson")),
        CategoriesOfPersonalDataJson = reader.GetString(reader.GetOrdinal("CategoriesOfPersonalDataJson")),
        RecipientsJson = reader.GetString(reader.GetOrdinal("RecipientsJson")),
        ThirdCountryTransfers = reader.IsDBNull(reader.GetOrdinal("ThirdCountryTransfers")) ? null : reader.GetString(reader.GetOrdinal("ThirdCountryTransfers")),
        Safeguards = reader.IsDBNull(reader.GetOrdinal("Safeguards")) ? null : reader.GetString(reader.GetOrdinal("Safeguards")),
        RetentionPeriodTicks = reader.GetInt64(reader.GetOrdinal("RetentionPeriodTicks")),
        SecurityMeasures = reader.GetString(reader.GetOrdinal("SecurityMeasures")),
        CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("CreatedAtUtc")), CultureInfo.InvariantCulture),
        LastUpdatedAtUtc = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("LastUpdatedAtUtc")), CultureInfo.InvariantCulture)
    };
}
