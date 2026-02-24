using System.Globalization;

using Dapper;

using Encina.Compliance.GDPR;

using LanguageExt;

using Microsoft.Data.Sqlite;

using static LanguageExt.Prelude;

namespace Encina.Dapper.Sqlite.LawfulBasis;

/// <summary>
/// Dapper implementation of <see cref="ILIAStore"/> for SQLite.
/// Uses parameterized queries with per-operation connections for thread safety.
/// </summary>
/// <remarks>
/// <para>
/// DateTimeOffset values (<c>AssessedAtUtc</c>, <c>NextReviewAtUtc</c>) are stored as
/// ISO 8601 strings (round-trip "O" format) because SQLite does not have a native
/// DateTimeOffset type.
/// </para>
/// <para>
/// Boolean values (<c>DPOInvolvement</c>) are stored as INTEGER (0/1) per SQLite conventions.
/// </para>
/// <para>
/// Collection properties (<c>AlternativesConsideredJson</c>, <c>SafeguardsJson</c>) are
/// serialized as JSON strings by <see cref="LIARecordMapper"/>.
/// </para>
/// </remarks>
public sealed class LIAStoreDapper : ILIAStore
{
    private const string TableName = "LIARecords";

    private const string ColumnList =
        "Id, Name, Purpose, LegitimateInterest, Benefits, ConsequencesIfNotProcessed, " +
        "NecessityJustification, AlternativesConsideredJson, DataMinimisationNotes, " +
        "NatureOfData, ReasonableExpectations, ImpactAssessment, SafeguardsJson, " +
        "OutcomeValue, Conclusion, Conditions, AssessedAtUtc, AssessedBy, DPOInvolvement, NextReviewAtUtc";

    private const string ParameterList =
        "@Id, @Name, @Purpose, @LegitimateInterest, @Benefits, @ConsequencesIfNotProcessed, " +
        "@NecessityJustification, @AlternativesConsideredJson, @DataMinimisationNotes, " +
        "@NatureOfData, @ReasonableExpectations, @ImpactAssessment, @SafeguardsJson, " +
        "@OutcomeValue, @Conclusion, @Conditions, @AssessedAtUtc, @AssessedBy, @DPOInvolvement, @NextReviewAtUtc";

    private const string InsertSql =
        $"INSERT OR REPLACE INTO {TableName} ({ColumnList}) VALUES ({ParameterList})";

    private const string SelectByReferenceSql =
        $"SELECT {ColumnList} FROM {TableName} WHERE Id = @Id";

    private const string SelectPendingReviewSql =
        $"SELECT {ColumnList} FROM {TableName} WHERE OutcomeValue = @OutcomeValue";

    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="LIAStoreDapper"/> class.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string.</param>
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

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await connection.ExecuteAsync(InsertSql, new
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
                AssessedAtUtc = entity.AssessedAtUtc.ToString("O"),
                entity.AssessedBy,
                DPOInvolvement = entity.DPOInvolvement ? 1 : 0,
                NextReviewAtUtc = entity.NextReviewAtUtc?.ToString("O")
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
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var rows = await connection.QueryAsync(
                SelectByReferenceSql,
                new { Id = liaReference }).ConfigureAwait(false);

            var row = rows.FirstOrDefault();
            if (row is null)
            {
                return Right<EncinaError, Option<LIARecord>>(None);
            }

            var entity = MapToEntity(row);
            var domain = LIARecordMapper.ToDomain(entity);

            return Right<EncinaError, Option<LIARecord>>(Some(domain));
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
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var rows = await connection.QueryAsync(
                SelectPendingReviewSql,
                new { OutcomeValue = (int)LIAOutcome.RequiresReview }).ConfigureAwait(false);

            var records = new List<LIARecord>();
            foreach (var row in rows)
            {
                var entity = MapToEntity(row);
                var domain = LIARecordMapper.ToDomain(entity);
                records.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<LIARecord>>(records.AsReadOnly());
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LIAStoreError("GetPendingReview", ex.Message));
        }
    }

    private static LIARecordEntity MapToEntity(dynamic row) => new()
    {
        Id = (string)row.Id,
        Name = (string)row.Name,
        Purpose = (string)row.Purpose,
        LegitimateInterest = (string)row.LegitimateInterest,
        Benefits = (string)row.Benefits,
        ConsequencesIfNotProcessed = (string)row.ConsequencesIfNotProcessed,
        NecessityJustification = (string)row.NecessityJustification,
        AlternativesConsideredJson = (string)row.AlternativesConsideredJson,
        DataMinimisationNotes = (string)row.DataMinimisationNotes,
        NatureOfData = (string)row.NatureOfData,
        ReasonableExpectations = (string)row.ReasonableExpectations,
        ImpactAssessment = (string)row.ImpactAssessment,
        SafeguardsJson = (string)row.SafeguardsJson,
        OutcomeValue = (int)(long)row.OutcomeValue,
        Conclusion = (string)row.Conclusion,
        Conditions = row.Conditions is null or DBNull ? null : (string)row.Conditions,
        AssessedAtUtc = DateTimeOffset.Parse((string)row.AssessedAtUtc, null, DateTimeStyles.RoundtripKind),
        AssessedBy = (string)row.AssessedBy,
        DPOInvolvement = (long)row.DPOInvolvement != 0,
        NextReviewAtUtc = row.NextReviewAtUtc is null or DBNull
            ? null
            : DateTimeOffset.Parse((string)row.NextReviewAtUtc, null, DateTimeStyles.RoundtripKind)
    };
}
