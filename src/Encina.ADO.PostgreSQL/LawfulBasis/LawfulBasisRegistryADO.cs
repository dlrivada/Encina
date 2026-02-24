using Encina.Compliance.GDPR;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.ADO.PostgreSQL.LawfulBasis;

/// <summary>
/// ADO.NET implementation of <see cref="ILawfulBasisRegistry"/> for PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates a new <see cref="NpgsqlConnection"/> per operation, making it
/// safe for singleton registration. Uses <c>ON CONFLICT</c> for upsert semantics on <c>requesttypename</c>.
/// </para>
/// <para>
/// Register via <c>AddEncinaLawfulBasisADOPostgreSQL(connectionString)</c>.
/// </para>
/// </remarks>
public sealed class LawfulBasisRegistryADO : ILawfulBasisRegistry
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="LawfulBasisRegistryADO"/> class.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    public LawfulBasisRegistryADO(string connectionString)
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
                INSERT INTO lawfulbasisregistrations
                    (id, requesttypename, basisvalue, purpose, liareference, legalreference, contractreference, registeredatutc)
                VALUES
                    (@requesttypename_id, @requesttypename, @basisvalue, @purpose, @liareference, @legalreference, @contractreference, @registeredatutc)
                ON CONFLICT (requesttypename) DO UPDATE SET
                    basisvalue = @basisvalue,
                    purpose = @purpose,
                    liareference = @liareference,
                    legalreference = @legalreference,
                    contractreference = @contractreference,
                    registeredatutc = @registeredatutc
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddRegistrationParameters(command, entity);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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
                SELECT id, requesttypename, basisvalue, purpose, liareference, legalreference, contractreference, registeredatutc
                FROM lawfulbasisregistrations
                WHERE requesttypename = @requesttypename
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@requesttypename", requestTypeName);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var entity = MapToEntity(reader);
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
                SELECT id, requesttypename, basisvalue, purpose, liareference, legalreference, contractreference, registeredatutc
                FROM lawfulbasisregistrations
                """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            var results = new List<LawfulBasisRegistration>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var entity = MapToEntity(reader);
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

    private static void AddRegistrationParameters(NpgsqlCommand command, LawfulBasisRegistrationEntity entity)
    {
        command.Parameters.AddWithValue("@requesttypename_id", entity.Id);
        command.Parameters.AddWithValue("@requesttypename", entity.RequestTypeName);
        command.Parameters.AddWithValue("@basisvalue", entity.BasisValue);
        command.Parameters.AddWithValue("@purpose", (object?)entity.Purpose ?? DBNull.Value);
        command.Parameters.AddWithValue("@liareference", (object?)entity.LIAReference ?? DBNull.Value);
        command.Parameters.AddWithValue("@legalreference", (object?)entity.LegalReference ?? DBNull.Value);
        command.Parameters.AddWithValue("@contractreference", (object?)entity.ContractReference ?? DBNull.Value);
        command.Parameters.AddWithValue("@registeredatutc", entity.RegisteredAtUtc);
    }

    private static LawfulBasisRegistrationEntity MapToEntity(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetString(reader.GetOrdinal("id")),
        RequestTypeName = reader.GetString(reader.GetOrdinal("requesttypename")),
        BasisValue = reader.GetInt32(reader.GetOrdinal("basisvalue")),
        Purpose = reader.IsDBNull(reader.GetOrdinal("purpose")) ? null : reader.GetString(reader.GetOrdinal("purpose")),
        LIAReference = reader.IsDBNull(reader.GetOrdinal("liareference")) ? null : reader.GetString(reader.GetOrdinal("liareference")),
        LegalReference = reader.IsDBNull(reader.GetOrdinal("legalreference")) ? null : reader.GetString(reader.GetOrdinal("legalreference")),
        ContractReference = reader.IsDBNull(reader.GetOrdinal("contractreference")) ? null : reader.GetString(reader.GetOrdinal("contractreference")),
        RegisteredAtUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("registeredatutc"))
    };
}
