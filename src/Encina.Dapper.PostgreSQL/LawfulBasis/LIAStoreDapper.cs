using Dapper;
using Encina.Compliance.GDPR;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.LawfulBasis;

/// <summary>
/// Dapper implementation of <see cref="ILIAStore"/> for PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates a new <see cref="NpgsqlConnection"/> per operation, making it
/// safe for singleton registration. Uses INSERT ... ON CONFLICT for upsert semantics on <c>id</c>.
/// </para>
/// <para>
/// Register via <c>AddEncinaLawfulBasisDapperPostgreSQL(connectionString)</c>.
/// </para>
/// </remarks>
public sealed class LIAStoreDapper : ILIAStore
{
    private const string InsertColumnList =
        "id, name, purpose, legitimate_interest, benefits, consequences_if_not_processed, " +
        "necessity_justification, alternatives_considered_json, data_minimisation_notes, " +
        "nature_of_data, reasonable_expectations, impact_assessment, safeguards_json, " +
        "outcome_value, conclusion, conditions, assessed_at_utc, assessed_by, " +
        "dpo_involvement, next_review_at_utc";

    private const string ParameterList =
        "@Id, @Name, @Purpose, @LegitimateInterest, @Benefits, @ConsequencesIfNotProcessed, " +
        "@NecessityJustification, @AlternativesConsideredJson, @DataMinimisationNotes, " +
        "@NatureOfData, @ReasonableExpectations, @ImpactAssessment, @SafeguardsJson, " +
        "@OutcomeValue, @Conclusion, @Conditions, @AssessedAtUtc, @AssessedBy, " +
        "@DPOInvolvement, @NextReviewAtUtc";

    private const string UpdateSet =
        "name = @Name, purpose = @Purpose, legitimate_interest = @LegitimateInterest, " +
        "benefits = @Benefits, consequences_if_not_processed = @ConsequencesIfNotProcessed, " +
        "necessity_justification = @NecessityJustification, alternatives_considered_json = @AlternativesConsideredJson, " +
        "data_minimisation_notes = @DataMinimisationNotes, nature_of_data = @NatureOfData, " +
        "reasonable_expectations = @ReasonableExpectations, impact_assessment = @ImpactAssessment, " +
        "safeguards_json = @SafeguardsJson, outcome_value = @OutcomeValue, conclusion = @Conclusion, " +
        "conditions = @Conditions, assessed_at_utc = @AssessedAtUtc, assessed_by = @AssessedBy, " +
        "dpo_involvement = @DPOInvolvement, next_review_at_utc = @NextReviewAtUtc";

    private const string SelectColumns =
        """id AS "Id", name AS "Name", purpose AS "Purpose", legitimate_interest AS "LegitimateInterest", benefits AS "Benefits", consequences_if_not_processed AS "ConsequencesIfNotProcessed", necessity_justification AS "NecessityJustification", alternatives_considered_json AS "AlternativesConsideredJson", data_minimisation_notes AS "DataMinimisationNotes", nature_of_data AS "NatureOfData", reasonable_expectations AS "ReasonableExpectations", impact_assessment AS "ImpactAssessment", safeguards_json AS "SafeguardsJson", outcome_value AS "OutcomeValue", conclusion AS "Conclusion", conditions AS "Conditions", assessed_at_utc AS "AssessedAtUtc", assessed_by AS "AssessedBy", dpo_involvement AS "DPOInvolvement", next_review_at_utc AS "NextReviewAtUtc" """;

    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="LIAStoreDapper"/> class.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    public LIAStoreDapper(string connectionString)
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
                INSERT INTO lia_records ({InsertColumnList})
                VALUES ({ParameterList})
                ON CONFLICT (id) DO UPDATE SET {UpdateSet}
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.Name,
                entity.Purpose,
                entity.LegitimateInterest,
                entity.Benefits,
                entity.ConsequencesIfNotProcessed,
                entity.NecessityJustification,
                entity.AlternativesConsideredJson,
                entity.DataMinimisationNotes,
                entity.NatureOfData,
                entity.ReasonableExpectations,
                entity.ImpactAssessment,
                entity.SafeguardsJson,
                entity.OutcomeValue,
                entity.Conclusion,
                entity.Conditions,
                entity.AssessedAtUtc,
                entity.AssessedBy,
                entity.DPOInvolvement,
                entity.NextReviewAtUtc
            }).ConfigureAwait(false);
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
                SELECT {SelectColumns}
                FROM lia_records
                WHERE id = @Id
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var entity = await connection.QueryFirstOrDefaultAsync<LIARecordEntity>(
                sql, new { Id = liaReference }).ConfigureAwait(false);

            if (entity is not null)
            {
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
                SELECT {SelectColumns}
                FROM lia_records
                WHERE outcome_value = @OutcomeValue
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var entities = await connection.QueryAsync<LIARecordEntity>(
                sql, new { OutcomeValue = (int)LIAOutcome.RequiresReview }).ConfigureAwait(false);

            var results = entities.Select(LIARecordMapper.ToDomain).ToList();
            return Right<EncinaError, IReadOnlyList<LIARecord>>(results);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LIAStoreError("GetPendingReview", ex.Message));
        }
    }
}
