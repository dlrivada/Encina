using Dapper;
using Encina.Compliance.GDPR;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.LawfulBasis;

/// <summary>
/// Dapper implementation of <see cref="ILawfulBasisRegistry"/> for PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates a new <see cref="NpgsqlConnection"/> per operation, making it
/// safe for singleton registration. Uses INSERT ... ON CONFLICT for upsert semantics on <c>request_type_name</c>.
/// </para>
/// <para>
/// Register via <c>AddEncinaLawfulBasisDapperPostgreSQL(connectionString)</c>.
/// </para>
/// </remarks>
public sealed class LawfulBasisRegistryDapper : ILawfulBasisRegistry
{
    private const string SelectColumns =
        """id AS "Id", request_type_name AS "RequestTypeName", basis_value AS "BasisValue", purpose AS "Purpose", lia_reference AS "LIAReference", legal_reference AS "LegalReference", contract_reference AS "ContractReference", registered_at_utc AS "RegisteredAtUtc" """;

    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="LawfulBasisRegistryDapper"/> class.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
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

            const string sql = """
                INSERT INTO lawful_basis_registrations (id, request_type_name, basis_value, purpose, lia_reference, legal_reference, contract_reference, registered_at_utc)
                VALUES (@Id, @RequestTypeName, @BasisValue, @Purpose, @LIAReference, @LegalReference, @ContractReference, @RegisteredAtUtc)
                ON CONFLICT (request_type_name) DO UPDATE SET
                    basis_value = @BasisValue,
                    purpose = @Purpose,
                    lia_reference = @LIAReference,
                    legal_reference = @LegalReference,
                    contract_reference = @ContractReference,
                    registered_at_utc = @RegisteredAtUtc
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.RequestTypeName,
                entity.BasisValue,
                entity.Purpose,
                entity.LIAReference,
                entity.LegalReference,
                entity.ContractReference,
                entity.RegisteredAtUtc
            }).ConfigureAwait(false);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LawfulBasisStoreError("Register", ex.Message));
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<LawfulBasisRegistration>>> GetByRequestTypeAsync(
        Type requestType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        return GetByRequestTypeNameAsync(requestType.AssemblyQualifiedName!, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<LawfulBasisRegistration>>> GetByRequestTypeNameAsync(
        string requestTypeName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestTypeName);

        try
        {
            var sql = $"""
                SELECT {SelectColumns}
                FROM lawful_basis_registrations
                WHERE request_type_name = @RequestTypeName
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var entity = await connection.QueryFirstOrDefaultAsync<LawfulBasisRegistrationEntity>(
                sql, new { RequestTypeName = requestTypeName }).ConfigureAwait(false);

            if (entity is not null)
            {
                var domain = LawfulBasisRegistrationMapper.ToDomain(entity);
                return domain is not null
                    ? Right<EncinaError, Option<LawfulBasisRegistration>>(Some(domain))
                    : Right<EncinaError, Option<LawfulBasisRegistration>>(None);
            }

            return Right<EncinaError, Option<LawfulBasisRegistration>>(None);
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
            var sql = $"""
                SELECT {SelectColumns}
                FROM lawful_basis_registrations
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var entities = await connection.QueryAsync<LawfulBasisRegistrationEntity>(sql).ConfigureAwait(false);

            var results = new List<LawfulBasisRegistration>();
            foreach (var entity in entities)
            {
                var domain = LawfulBasisRegistrationMapper.ToDomain(entity);
                if (domain is not null)
                {
                    results.Add(domain);
                }
            }

            return Right<EncinaError, IReadOnlyList<LawfulBasisRegistration>>(results);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LawfulBasisStoreError("GetAll", ex.Message));
        }
    }
}
