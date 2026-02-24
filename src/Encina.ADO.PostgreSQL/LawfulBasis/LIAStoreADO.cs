using Encina.Compliance.GDPR;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.ADO.PostgreSQL.LawfulBasis;

/// <summary>
/// ADO.NET implementation of <see cref="ILIAStore"/> for PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates a new <see cref="NpgsqlConnection"/> per operation, making it
/// safe for singleton registration. Uses <c>ON CONFLICT</c> for upsert semantics on <c>id</c>.
/// </para>
/// <para>
/// Register via <c>AddEncinaLawfulBasisADOPostgreSQL(connectionString)</c>.
/// </para>
/// </remarks>
public sealed class LIAStoreADO : ILIAStore
{
    private const string ColumnList =
        "id, name, purpose, legitimateinterest, benefits, consequencesifnotprocessed, " +
        "necessityjustification, alternativesconsideredjson, dataminimisationnotes, " +
        "natureofdata, reasonableexpectations, impactassessment, safeguardsjson, " +
        "outcomevalue, conclusion, conditions, assessedatutc, assessedby, " +
        "dpoinvolvement, nextreviewatutc";

    private const string ParameterList =
        "@id, @name, @purpose, @legitimateinterest, @benefits, @consequencesifnotprocessed, " +
        "@necessityjustification, @alternativesconsideredjson, @dataminimisationnotes, " +
        "@natureofdata, @reasonableexpectations, @impactassessment, @safeguardsjson, " +
        "@outcomevalue, @conclusion, @conditions, @assessedatutc, @assessedby, " +
        "@dpoinvolvement, @nextreviewatutc";

    private const string UpdateSet =
        "name = @name, purpose = @purpose, legitimateinterest = @legitimateinterest, " +
        "benefits = @benefits, consequencesifnotprocessed = @consequencesifnotprocessed, " +
        "necessityjustification = @necessityjustification, alternativesconsideredjson = @alternativesconsideredjson, " +
        "dataminimisationnotes = @dataminimisationnotes, natureofdata = @natureofdata, " +
        "reasonableexpectations = @reasonableexpectations, impactassessment = @impactassessment, " +
        "safeguardsjson = @safeguardsjson, outcomevalue = @outcomevalue, conclusion = @conclusion, " +
        "conditions = @conditions, assessedatutc = @assessedatutc, assessedby = @assessedby, " +
        "dpoinvolvement = @dpoinvolvement, nextreviewatutc = @nextreviewatutc";

    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="LIAStoreADO"/> class.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    public LIAStoreADO(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> StoreAsync(
        LIARecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        try
        {
            var entity = LIARecordMapper.ToEntity(record);

            var sql = $"""
                INSERT INTO liarecords ({ColumnList})
                VALUES ({ParameterList})
                ON CONFLICT (id) DO UPDATE SET {UpdateSet}
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddLIAParameters(command, entity);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LIAStoreError("Store", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<LIARecord>>> GetByReferenceAsync(
        string liaReference,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(liaReference);

        try
        {
            var sql = $"""
                SELECT {ColumnList}
                FROM liarecords
                WHERE id = @id
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@id", liaReference);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var entity = MapToEntity(reader);
                var domain = LIARecordMapper.ToDomain(entity);
                return Right<EncinaError, Option<LIARecord>>(Some(domain));
            }

            return Right<EncinaError, Option<LIARecord>>(None);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LIAStoreError("GetByReference", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<LIARecord>>> GetPendingReviewAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $"""
                SELECT {ColumnList}
                FROM liarecords
                WHERE outcomevalue = @outcomevalue
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@outcomevalue", (int)LIAOutcome.RequiresReview);

            var results = new List<LIARecord>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var entity = MapToEntity(reader);
                results.Add(LIARecordMapper.ToDomain(entity));
            }

            return Right<EncinaError, IReadOnlyList<LIARecord>>(results);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LIAStoreError("GetPendingReview", ex.Message));
        }
    }

    private static void AddLIAParameters(NpgsqlCommand command, LIARecordEntity entity)
    {
        command.Parameters.AddWithValue("@id", entity.Id);
        command.Parameters.AddWithValue("@name", entity.Name);
        command.Parameters.AddWithValue("@purpose", entity.Purpose);
        command.Parameters.AddWithValue("@legitimateinterest", entity.LegitimateInterest);
        command.Parameters.AddWithValue("@benefits", entity.Benefits);
        command.Parameters.AddWithValue("@consequencesifnotprocessed", entity.ConsequencesIfNotProcessed);
        command.Parameters.AddWithValue("@necessityjustification", entity.NecessityJustification);
        command.Parameters.AddWithValue("@alternativesconsideredjson", entity.AlternativesConsideredJson);
        command.Parameters.AddWithValue("@dataminimisationnotes", entity.DataMinimisationNotes);
        command.Parameters.AddWithValue("@natureofdata", entity.NatureOfData);
        command.Parameters.AddWithValue("@reasonableexpectations", entity.ReasonableExpectations);
        command.Parameters.AddWithValue("@impactassessment", entity.ImpactAssessment);
        command.Parameters.AddWithValue("@safeguardsjson", entity.SafeguardsJson);
        command.Parameters.AddWithValue("@outcomevalue", entity.OutcomeValue);
        command.Parameters.AddWithValue("@conclusion", entity.Conclusion);
        command.Parameters.AddWithValue("@conditions", (object?)entity.Conditions ?? DBNull.Value);
        command.Parameters.AddWithValue("@assessedatutc", entity.AssessedAtUtc);
        command.Parameters.AddWithValue("@assessedby", entity.AssessedBy);
        command.Parameters.AddWithValue("@dpoinvolvement", entity.DPOInvolvement);
        command.Parameters.AddWithValue("@nextreviewatutc", entity.NextReviewAtUtc.HasValue ? entity.NextReviewAtUtc.Value : DBNull.Value);
    }

    private static LIARecordEntity MapToEntity(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetString(reader.GetOrdinal("id")),
        Name = reader.GetString(reader.GetOrdinal("name")),
        Purpose = reader.GetString(reader.GetOrdinal("purpose")),
        LegitimateInterest = reader.GetString(reader.GetOrdinal("legitimateinterest")),
        Benefits = reader.GetString(reader.GetOrdinal("benefits")),
        ConsequencesIfNotProcessed = reader.GetString(reader.GetOrdinal("consequencesifnotprocessed")),
        NecessityJustification = reader.GetString(reader.GetOrdinal("necessityjustification")),
        AlternativesConsideredJson = reader.GetString(reader.GetOrdinal("alternativesconsideredjson")),
        DataMinimisationNotes = reader.GetString(reader.GetOrdinal("dataminimisationnotes")),
        NatureOfData = reader.GetString(reader.GetOrdinal("natureofdata")),
        ReasonableExpectations = reader.GetString(reader.GetOrdinal("reasonableexpectations")),
        ImpactAssessment = reader.GetString(reader.GetOrdinal("impactassessment")),
        SafeguardsJson = reader.GetString(reader.GetOrdinal("safeguardsjson")),
        OutcomeValue = reader.GetInt32(reader.GetOrdinal("outcomevalue")),
        Conclusion = reader.GetString(reader.GetOrdinal("conclusion")),
        Conditions = reader.IsDBNull(reader.GetOrdinal("conditions")) ? null : reader.GetString(reader.GetOrdinal("conditions")),
        AssessedAtUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("assessedatutc")),
        AssessedBy = reader.GetString(reader.GetOrdinal("assessedby")),
        DPOInvolvement = reader.GetBoolean(reader.GetOrdinal("dpoinvolvement")),
        NextReviewAtUtc = reader.IsDBNull(reader.GetOrdinal("nextreviewatutc")) ? null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("nextreviewatutc"))
    };
}
