using System.Globalization;
using Encina.Compliance.GDPR;
using LanguageExt;
using Microsoft.Data.Sqlite;
using static LanguageExt.Prelude;

namespace Encina.ADO.Sqlite.LawfulBasis;

/// <summary>
/// ADO.NET implementation of <see cref="ILIAStore"/> for SQLite.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates a new <see cref="SqliteConnection"/> per operation, making it
/// safe for singleton registration. Uses <c>INSERT OR REPLACE</c> for upsert semantics
/// on <c>Id</c>.
/// </para>
/// <para>
/// Register via <c>AddEncinaLawfulBasisADOSqlite(connectionString)</c>.
/// </para>
/// </remarks>
public sealed class LIAStoreADO : ILIAStore
{
    private const string ColumnList =
        "Id, Name, Purpose, LegitimateInterest, Benefits, ConsequencesIfNotProcessed, " +
        "NecessityJustification, AlternativesConsideredJson, DataMinimisationNotes, " +
        "NatureOfData, ReasonableExpectations, ImpactAssessment, SafeguardsJson, " +
        "OutcomeValue, Conclusion, Conditions, AssessedAtUtc, AssessedBy, " +
        "DPOInvolvement, NextReviewAtUtc";

    private const string ParameterList =
        "@Id, @Name, @Purpose, @LegitimateInterest, @Benefits, @ConsequencesIfNotProcessed, " +
        "@NecessityJustification, @AlternativesConsideredJson, @DataMinimisationNotes, " +
        "@NatureOfData, @ReasonableExpectations, @ImpactAssessment, @SafeguardsJson, " +
        "@OutcomeValue, @Conclusion, @Conditions, @AssessedAtUtc, @AssessedBy, " +
        "@DPOInvolvement, @NextReviewAtUtc";

    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="LIAStoreADO"/> class.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string.</param>
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
                INSERT OR REPLACE INTO LIARecords
                    ({ColumnList})
                VALUES
                    ({ParameterList})
                """;

            await using var connection = new SqliteConnection(_connectionString);
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
                FROM LIARecords
                WHERE Id = @Id
                """;

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@Id", liaReference);

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
                FROM LIARecords
                WHERE OutcomeValue = @OutcomeValue
                """;

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@OutcomeValue", (int)LIAOutcome.RequiresReview);

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

    private static void AddLIAParameters(SqliteCommand command, LIARecordEntity entity)
    {
        command.Parameters.AddWithValue("@Id", entity.Id);
        command.Parameters.AddWithValue("@Name", entity.Name);
        command.Parameters.AddWithValue("@Purpose", entity.Purpose);
        command.Parameters.AddWithValue("@LegitimateInterest", entity.LegitimateInterest);
        command.Parameters.AddWithValue("@Benefits", entity.Benefits);
        command.Parameters.AddWithValue("@ConsequencesIfNotProcessed", entity.ConsequencesIfNotProcessed);
        command.Parameters.AddWithValue("@NecessityJustification", entity.NecessityJustification);
        command.Parameters.AddWithValue("@AlternativesConsideredJson", entity.AlternativesConsideredJson);
        command.Parameters.AddWithValue("@DataMinimisationNotes", entity.DataMinimisationNotes);
        command.Parameters.AddWithValue("@NatureOfData", entity.NatureOfData);
        command.Parameters.AddWithValue("@ReasonableExpectations", entity.ReasonableExpectations);
        command.Parameters.AddWithValue("@ImpactAssessment", entity.ImpactAssessment);
        command.Parameters.AddWithValue("@SafeguardsJson", entity.SafeguardsJson);
        command.Parameters.AddWithValue("@OutcomeValue", entity.OutcomeValue);
        command.Parameters.AddWithValue("@Conclusion", entity.Conclusion);
        command.Parameters.AddWithValue("@Conditions", (object?)entity.Conditions ?? DBNull.Value);
        command.Parameters.AddWithValue("@AssessedAtUtc", entity.AssessedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("@AssessedBy", entity.AssessedBy);
        command.Parameters.AddWithValue("@DPOInvolvement", entity.DPOInvolvement ? 1 : 0);
        command.Parameters.AddWithValue("@NextReviewAtUtc",
            entity.NextReviewAtUtc.HasValue ? entity.NextReviewAtUtc.Value.ToString("O") : DBNull.Value);
    }

    private static LIARecordEntity MapToEntity(SqliteDataReader reader) => new()
    {
        Id = reader.GetString(reader.GetOrdinal("Id")),
        Name = reader.GetString(reader.GetOrdinal("Name")),
        Purpose = reader.GetString(reader.GetOrdinal("Purpose")),
        LegitimateInterest = reader.GetString(reader.GetOrdinal("LegitimateInterest")),
        Benefits = reader.GetString(reader.GetOrdinal("Benefits")),
        ConsequencesIfNotProcessed = reader.GetString(reader.GetOrdinal("ConsequencesIfNotProcessed")),
        NecessityJustification = reader.GetString(reader.GetOrdinal("NecessityJustification")),
        AlternativesConsideredJson = reader.GetString(reader.GetOrdinal("AlternativesConsideredJson")),
        DataMinimisationNotes = reader.GetString(reader.GetOrdinal("DataMinimisationNotes")),
        NatureOfData = reader.GetString(reader.GetOrdinal("NatureOfData")),
        ReasonableExpectations = reader.GetString(reader.GetOrdinal("ReasonableExpectations")),
        ImpactAssessment = reader.GetString(reader.GetOrdinal("ImpactAssessment")),
        SafeguardsJson = reader.GetString(reader.GetOrdinal("SafeguardsJson")),
        OutcomeValue = reader.GetInt32(reader.GetOrdinal("OutcomeValue")),
        Conclusion = reader.GetString(reader.GetOrdinal("Conclusion")),
        Conditions = reader.IsDBNull(reader.GetOrdinal("Conditions")) ? null : reader.GetString(reader.GetOrdinal("Conditions")),
        AssessedAtUtc = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("AssessedAtUtc")), CultureInfo.InvariantCulture),
        AssessedBy = reader.GetString(reader.GetOrdinal("AssessedBy")),
        DPOInvolvement = reader.GetInt32(reader.GetOrdinal("DPOInvolvement")) == 1,
        NextReviewAtUtc = reader.IsDBNull(reader.GetOrdinal("NextReviewAtUtc"))
            ? null
            : DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("NextReviewAtUtc")), CultureInfo.InvariantCulture)
    };
}
