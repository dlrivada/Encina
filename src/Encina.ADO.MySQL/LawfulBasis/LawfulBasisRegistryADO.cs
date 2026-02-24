using Encina.Compliance.GDPR;
using LanguageExt;
using MySqlConnector;
using static LanguageExt.Prelude;

namespace Encina.ADO.MySQL.LawfulBasis;

/// <summary>
/// ADO.NET implementation of <see cref="ILawfulBasisRegistry"/> for MySQL.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates a new <see cref="MySqlConnection"/> per operation, making it
/// safe for singleton registration. Uses <c>INSERT ... ON DUPLICATE KEY UPDATE</c> for upsert
/// semantics on <c>RequestTypeName</c>.
/// </para>
/// <para>
/// Register via <c>AddEncinaLawfulBasisADOMySQL(connectionString)</c>.
/// </para>
/// </remarks>
public sealed class LawfulBasisRegistryADO : ILawfulBasisRegistry
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="LawfulBasisRegistryADO"/> class.
    /// </summary>
    /// <param name="connectionString">The MySQL connection string.</param>
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
                INSERT INTO `LawfulBasisRegistrations`
                    (`Id`, `RequestTypeName`, `BasisValue`, `Purpose`, `LIAReference`, `LegalReference`, `ContractReference`, `RegisteredAtUtc`)
                VALUES
                    (@Id, @RequestTypeName, @BasisValue, @Purpose, @LIAReference, @LegalReference, @ContractReference, @RegisteredAtUtc)
                ON DUPLICATE KEY UPDATE
                    `BasisValue` = @BasisValue,
                    `Purpose` = @Purpose,
                    `LIAReference` = @LIAReference,
                    `LegalReference` = @LegalReference,
                    `ContractReference` = @ContractReference,
                    `RegisteredAtUtc` = @RegisteredAtUtc
                """;

            await using var connection = new MySqlConnection(_connectionString);
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
                SELECT `Id`, `RequestTypeName`, `BasisValue`, `Purpose`, `LIAReference`, `LegalReference`, `ContractReference`, `RegisteredAtUtc`
                FROM `LawfulBasisRegistrations`
                WHERE `RequestTypeName` = @RequestTypeName
                """;

            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@RequestTypeName", requestTypeName);

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
                SELECT `Id`, `RequestTypeName`, `BasisValue`, `Purpose`, `LIAReference`, `LegalReference`, `ContractReference`, `RegisteredAtUtc`
                FROM `LawfulBasisRegistrations`
                """;

            await using var connection = new MySqlConnection(_connectionString);
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

    private static void AddRegistrationParameters(MySqlCommand command, LawfulBasisRegistrationEntity entity)
    {
        command.Parameters.AddWithValue("@Id", entity.Id);
        command.Parameters.AddWithValue("@RequestTypeName", entity.RequestTypeName);
        command.Parameters.AddWithValue("@BasisValue", entity.BasisValue);
        command.Parameters.AddWithValue("@Purpose", (object?)entity.Purpose ?? DBNull.Value);
        command.Parameters.AddWithValue("@LIAReference", (object?)entity.LIAReference ?? DBNull.Value);
        command.Parameters.AddWithValue("@LegalReference", (object?)entity.LegalReference ?? DBNull.Value);
        command.Parameters.AddWithValue("@ContractReference", (object?)entity.ContractReference ?? DBNull.Value);
        command.Parameters.AddWithValue("@RegisteredAtUtc", entity.RegisteredAtUtc);
    }

    private static LawfulBasisRegistrationEntity MapToEntity(MySqlDataReader reader) => new()
    {
        Id = reader.GetString(reader.GetOrdinal("Id")),
        RequestTypeName = reader.GetString(reader.GetOrdinal("RequestTypeName")),
        BasisValue = reader.GetInt32(reader.GetOrdinal("BasisValue")),
        Purpose = reader.IsDBNull(reader.GetOrdinal("Purpose")) ? null : reader.GetString(reader.GetOrdinal("Purpose")),
        LIAReference = reader.IsDBNull(reader.GetOrdinal("LIAReference")) ? null : reader.GetString(reader.GetOrdinal("LIAReference")),
        LegalReference = reader.IsDBNull(reader.GetOrdinal("LegalReference")) ? null : reader.GetString(reader.GetOrdinal("LegalReference")),
        ContractReference = reader.IsDBNull(reader.GetOrdinal("ContractReference")) ? null : reader.GetString(reader.GetOrdinal("ContractReference")),
        RegisteredAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("RegisteredAtUtc")), TimeSpan.Zero)
    };
}
