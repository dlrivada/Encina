using Dapper;
using Encina.Compliance.GDPR;
using LanguageExt;
using MySqlConnector;
using static LanguageExt.Prelude;

namespace Encina.Dapper.MySQL.LawfulBasis;

/// <summary>
/// Dapper implementation of <see cref="ILIAStore"/> for MySQL/MariaDB.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates a new <see cref="MySqlConnection"/> per operation, making it
/// safe for singleton registration. Uses <c>INSERT ... ON DUPLICATE KEY UPDATE</c> for upsert
/// semantics on <c>Id</c>.
/// </para>
/// <para>
/// Register via <c>AddEncinaLawfulBasisDapperMySQL(connectionString)</c>.
/// </para>
/// </remarks>
public sealed class LIAStoreDapper : ILIAStore
{
    private const string ColumnList =
        "`Id`, `Name`, `Purpose`, `LegitimateInterest`, `Benefits`, `ConsequencesIfNotProcessed`, " +
        "`NecessityJustification`, `AlternativesConsideredJson`, `DataMinimisationNotes`, " +
        "`NatureOfData`, `ReasonableExpectations`, `ImpactAssessment`, `SafeguardsJson`, " +
        "`OutcomeValue`, `Conclusion`, `Conditions`, `AssessedAtUtc`, `AssessedBy`, " +
        "`DPOInvolvement`, `NextReviewAtUtc`";

    private const string ParameterList =
        "@Id, @Name, @Purpose, @LegitimateInterest, @Benefits, @ConsequencesIfNotProcessed, " +
        "@NecessityJustification, @AlternativesConsideredJson, @DataMinimisationNotes, " +
        "@NatureOfData, @ReasonableExpectations, @ImpactAssessment, @SafeguardsJson, " +
        "@OutcomeValue, @Conclusion, @Conditions, @AssessedAtUtc, @AssessedBy, " +
        "@DPOInvolvement, @NextReviewAtUtc";

    private const string UpdateSet =
        "`Name` = @Name, `Purpose` = @Purpose, `LegitimateInterest` = @LegitimateInterest, " +
        "`Benefits` = @Benefits, `ConsequencesIfNotProcessed` = @ConsequencesIfNotProcessed, " +
        "`NecessityJustification` = @NecessityJustification, `AlternativesConsideredJson` = @AlternativesConsideredJson, " +
        "`DataMinimisationNotes` = @DataMinimisationNotes, `NatureOfData` = @NatureOfData, " +
        "`ReasonableExpectations` = @ReasonableExpectations, `ImpactAssessment` = @ImpactAssessment, " +
        "`SafeguardsJson` = @SafeguardsJson, `OutcomeValue` = @OutcomeValue, `Conclusion` = @Conclusion, " +
        "`Conditions` = @Conditions, `AssessedAtUtc` = @AssessedAtUtc, `AssessedBy` = @AssessedBy, " +
        "`DPOInvolvement` = @DPOInvolvement, `NextReviewAtUtc` = @NextReviewAtUtc";

    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="LIAStoreDapper"/> class.
    /// </summary>
    /// <param name="connectionString">The MySQL/MariaDB connection string.</param>
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
                INSERT INTO `LIARecords` ({ColumnList})
                VALUES ({ParameterList})
                ON DUPLICATE KEY UPDATE {UpdateSet}
                """;

            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
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
                FROM `LIARecords`
                WHERE `Id` = @Id
                """;

            await using var connection = new MySqlConnection(_connectionString);
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
                SELECT {ColumnList}
                FROM `LIARecords`
                WHERE `OutcomeValue` = @OutcomeValue
                """;

            await using var connection = new MySqlConnection(_connectionString);
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
