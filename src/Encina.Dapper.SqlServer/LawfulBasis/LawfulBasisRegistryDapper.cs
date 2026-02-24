using Dapper;
using Encina.Compliance.GDPR;
using LanguageExt;
using Microsoft.Data.SqlClient;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.LawfulBasis;

/// <summary>
/// Dapper implementation of <see cref="ILawfulBasisRegistry"/> for SQL Server.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates a new <see cref="SqlConnection"/> per operation, making it
/// safe for singleton registration. Uses MERGE for upsert semantics on <c>RequestTypeName</c>.
/// </para>
/// <para>
/// Register via <c>AddEncinaLawfulBasisDapperSqlServer(connectionString)</c>.
/// </para>
/// </remarks>
public sealed class LawfulBasisRegistryDapper : ILawfulBasisRegistry
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="LawfulBasisRegistryDapper"/> class.
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
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
                MERGE INTO [LawfulBasisRegistrations] AS target
                USING (SELECT @RequestTypeName AS [RequestTypeName]) AS source
                ON target.[RequestTypeName] = source.[RequestTypeName]
                WHEN MATCHED THEN
                    UPDATE SET
                        [BasisValue] = @BasisValue,
                        [Purpose] = @Purpose,
                        [LIAReference] = @LIAReference,
                        [LegalReference] = @LegalReference,
                        [ContractReference] = @ContractReference,
                        [RegisteredAtUtc] = @RegisteredAtUtc
                WHEN NOT MATCHED THEN
                    INSERT ([Id], [RequestTypeName], [BasisValue], [Purpose], [LIAReference], [LegalReference], [ContractReference], [RegisteredAtUtc])
                    VALUES (@Id, @RequestTypeName, @BasisValue, @Purpose, @LIAReference, @LegalReference, @ContractReference, @RegisteredAtUtc);
                """;

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
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
            const string sql = """
                SELECT [Id], [RequestTypeName], [BasisValue], [Purpose], [LIAReference], [LegalReference], [ContractReference], [RegisteredAtUtc]
                FROM [LawfulBasisRegistrations]
                WHERE [RequestTypeName] = @RequestTypeName
                """;

            await using var connection = new SqlConnection(_connectionString);
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
            const string sql = """
                SELECT [Id], [RequestTypeName], [BasisValue], [Purpose], [LIAReference], [LegalReference], [ContractReference], [RegisteredAtUtc]
                FROM [LawfulBasisRegistrations]
                """;

            await using var connection = new SqlConnection(_connectionString);
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
