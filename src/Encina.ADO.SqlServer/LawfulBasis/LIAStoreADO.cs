using System.Data;
using Encina.Compliance.GDPR;
using LanguageExt;
using Microsoft.Data.SqlClient;
using static LanguageExt.Prelude;

namespace Encina.ADO.SqlServer.LawfulBasis;

/// <summary>
/// ADO.NET implementation of <see cref="ILIAStore"/> for SQL Server.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates a new <see cref="SqlConnection"/> per operation, making it
/// safe for singleton registration. Uses MERGE for upsert semantics on <c>Id</c>.
/// </para>
/// <para>
/// Register via <c>AddEncinaLawfulBasisADOSqlServer(connectionString)</c>.
/// </para>
/// </remarks>
public sealed class LIAStoreADO : ILIAStore
{
    private const string ColumnList =
        "[Id], [Name], [Purpose], [LegitimateInterest], [Benefits], [ConsequencesIfNotProcessed], " +
        "[NecessityJustification], [AlternativesConsideredJson], [DataMinimisationNotes], " +
        "[NatureOfData], [ReasonableExpectations], [ImpactAssessment], [SafeguardsJson], " +
        "[OutcomeValue], [Conclusion], [Conditions], [AssessedAtUtc], [AssessedBy], " +
        "[DPOInvolvement], [NextReviewAtUtc]";

    private const string ParameterList =
        "@Id, @Name, @Purpose, @LegitimateInterest, @Benefits, @ConsequencesIfNotProcessed, " +
        "@NecessityJustification, @AlternativesConsideredJson, @DataMinimisationNotes, " +
        "@NatureOfData, @ReasonableExpectations, @ImpactAssessment, @SafeguardsJson, " +
        "@OutcomeValue, @Conclusion, @Conditions, @AssessedAtUtc, @AssessedBy, " +
        "@DPOInvolvement, @NextReviewAtUtc";

    private const string UpdateSet =
        "[Name] = @Name, [Purpose] = @Purpose, [LegitimateInterest] = @LegitimateInterest, " +
        "[Benefits] = @Benefits, [ConsequencesIfNotProcessed] = @ConsequencesIfNotProcessed, " +
        "[NecessityJustification] = @NecessityJustification, [AlternativesConsideredJson] = @AlternativesConsideredJson, " +
        "[DataMinimisationNotes] = @DataMinimisationNotes, [NatureOfData] = @NatureOfData, " +
        "[ReasonableExpectations] = @ReasonableExpectations, [ImpactAssessment] = @ImpactAssessment, " +
        "[SafeguardsJson] = @SafeguardsJson, [OutcomeValue] = @OutcomeValue, [Conclusion] = @Conclusion, " +
        "[Conditions] = @Conditions, [AssessedAtUtc] = @AssessedAtUtc, [AssessedBy] = @AssessedBy, " +
        "[DPOInvolvement] = @DPOInvolvement, [NextReviewAtUtc] = @NextReviewAtUtc";

    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="LIAStoreADO"/> class.
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
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
                MERGE INTO [LIARecords] AS target
                USING (SELECT @Id AS [Id]) AS source
                ON target.[Id] = source.[Id]
                WHEN MATCHED THEN
                    UPDATE SET {UpdateSet}
                WHEN NOT MATCHED THEN
                    INSERT ({ColumnList})
                    VALUES ({ParameterList});
                """;

            await using var connection = new SqlConnection(_connectionString);
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
                FROM [LIARecords]
                WHERE [Id] = @Id
                """;

            await using var connection = new SqlConnection(_connectionString);
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
                FROM [LIARecords]
                WHERE [OutcomeValue] = @OutcomeValue
                """;

            await using var connection = new SqlConnection(_connectionString);
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

    private static void AddLIAParameters(SqlCommand command, LIARecordEntity entity)
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
        command.Parameters.AddWithValue("@AssessedAtUtc", entity.AssessedAtUtc);
        command.Parameters.AddWithValue("@AssessedBy", entity.AssessedBy);
        command.Parameters.AddWithValue("@DPOInvolvement", entity.DPOInvolvement);
        command.Parameters.AddWithValue("@NextReviewAtUtc", entity.NextReviewAtUtc.HasValue ? entity.NextReviewAtUtc.Value : DBNull.Value);
    }

    private static LIARecordEntity MapToEntity(SqlDataReader reader) => new()
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
        AssessedAtUtc = (DateTimeOffset)reader.GetValue(reader.GetOrdinal("AssessedAtUtc")),
        AssessedBy = reader.GetString(reader.GetOrdinal("AssessedBy")),
        DPOInvolvement = reader.GetBoolean(reader.GetOrdinal("DPOInvolvement")),
        NextReviewAtUtc = reader.IsDBNull(reader.GetOrdinal("NextReviewAtUtc")) ? null : (DateTimeOffset)reader.GetValue(reader.GetOrdinal("NextReviewAtUtc"))
    };
}
