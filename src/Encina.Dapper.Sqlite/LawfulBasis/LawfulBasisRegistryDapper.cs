using System.Globalization;

using Dapper;

using Encina.Compliance.GDPR;

using LanguageExt;

using Microsoft.Data.Sqlite;

using static LanguageExt.Prelude;

namespace Encina.Dapper.Sqlite.LawfulBasis;

/// <summary>
/// Dapper implementation of <see cref="ILawfulBasisRegistry"/> for SQLite.
/// Uses parameterized queries with per-operation connections for thread safety.
/// </summary>
/// <remarks>
/// <para>
/// DateTimeOffset values are stored as ISO 8601 strings (round-trip "O" format)
/// because SQLite does not have a native DateTimeOffset type.
/// </para>
/// <para>
/// The <c>INSERT OR REPLACE</c> statement provides upsert semantics, allowing
/// re-registration of a request type to update its lawful basis declaration.
/// </para>
/// </remarks>
public sealed class LawfulBasisRegistryDapper : ILawfulBasisRegistry
{
    private const string TableName = "LawfulBasisRegistrations";

    private const string InsertSql = $"""
        INSERT OR REPLACE INTO {TableName}
        (Id, RequestTypeName, BasisValue, Purpose, LIAReference, LegalReference, ContractReference, RegisteredAtUtc)
        VALUES
        (@Id, @RequestTypeName, @BasisValue, @Purpose, @LIAReference, @LegalReference, @ContractReference, @RegisteredAtUtc)
        """;

    private const string SelectByRequestTypeNameSql = $"""
        SELECT Id, RequestTypeName, BasisValue, Purpose, LIAReference, LegalReference, ContractReference, RegisteredAtUtc
        FROM {TableName}
        WHERE RequestTypeName = @RequestTypeName
        """;

    private const string SelectAllSql = $"""
        SELECT Id, RequestTypeName, BasisValue, Purpose, LIAReference, LegalReference, ContractReference, RegisteredAtUtc
        FROM {TableName}
        """;

    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="LawfulBasisRegistryDapper"/> class.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string.</param>
    public LawfulBasisRegistryDapper(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RegisterAsync(
        LawfulBasisRegistration registration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);

        try
        {
            var entity = LawfulBasisRegistrationMapper.ToEntity(registration);

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await connection.ExecuteAsync(InsertSql, new
            {
                entity.Id,
                entity.RequestTypeName,
                entity.BasisValue,
                entity.Purpose,
                entity.LIAReference,
                entity.LegalReference,
                entity.ContractReference,
                RegisteredAtUtc = entity.RegisteredAtUtc.ToString("O")
            }).ConfigureAwait(false);

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LawfulBasisStoreError("Register", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<LawfulBasisRegistration>>> GetByRequestTypeAsync(
        Type requestType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        var requestTypeName = requestType.AssemblyQualifiedName!;
        return await GetByRequestTypeNameAsync(requestTypeName, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<LawfulBasisRegistration>>> GetByRequestTypeNameAsync(
        string requestTypeName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestTypeName);

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var rows = await connection.QueryAsync(
                SelectByRequestTypeNameSql,
                new { RequestTypeName = requestTypeName }).ConfigureAwait(false);

            var row = rows.FirstOrDefault();
            if (row is null)
            {
                return Right<EncinaError, Option<LawfulBasisRegistration>>(None);
            }

            var entity = MapToEntity(row);
            var domain = LawfulBasisRegistrationMapper.ToDomain(entity);

            return domain is not null
                ? Right<EncinaError, Option<LawfulBasisRegistration>>(Some(domain))
                : Right<EncinaError, Option<LawfulBasisRegistration>>(None);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LawfulBasisStoreError("GetByRequestTypeName", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<LawfulBasisRegistration>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var rows = await connection.QueryAsync(SelectAllSql).ConfigureAwait(false);

            var registrations = new List<LawfulBasisRegistration>();
            foreach (var row in rows)
            {
                var entity = MapToEntity(row);
                var domain = LawfulBasisRegistrationMapper.ToDomain(entity);
                if (domain is not null)
                {
                    registrations.Add(domain);
                }
            }

            return Right<EncinaError, IReadOnlyList<LawfulBasisRegistration>>(registrations.AsReadOnly());
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LawfulBasisStoreError("GetAll", ex.Message));
        }
    }

    private static LawfulBasisRegistrationEntity MapToEntity(dynamic row) => new()
    {
        Id = (string)row.Id,
        RequestTypeName = (string)row.RequestTypeName,
        BasisValue = (int)(long)row.BasisValue,
        Purpose = row.Purpose is null or DBNull ? null : (string)row.Purpose,
        LIAReference = row.LIAReference is null or DBNull ? null : (string)row.LIAReference,
        LegalReference = row.LegalReference is null or DBNull ? null : (string)row.LegalReference,
        ContractReference = row.ContractReference is null or DBNull ? null : (string)row.ContractReference,
        RegisteredAtUtc = DateTimeOffset.Parse((string)row.RegisteredAtUtc, null, DateTimeStyles.RoundtripKind)
    };
}
