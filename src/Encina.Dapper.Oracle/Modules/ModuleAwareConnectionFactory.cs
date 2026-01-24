using System.Data;
using System.Data.Common;
using Encina.Modules.Isolation;

namespace Encina.Dapper.Oracle.Modules;

/// <summary>
/// Factory that creates module-aware database connections with schema validation for Oracle.
/// </summary>
/// <remarks>
/// <para>
/// This factory wraps connection creation to optionally add schema validation.
/// When module isolation is enabled with <see cref="ModuleIsolationStrategy.DevelopmentValidationOnly"/>,
/// connections are wrapped in <see cref="SchemaValidatingConnection"/> which validates
/// SQL statements against module schema boundaries.
/// </para>
/// <para>
/// <b>Usage Patterns</b>:
/// <list type="bullet">
/// <item><description>Register as a factory function in DI</description></item>
/// <item><description>Inject and use to create connections in stores/repositories</description></item>
/// <item><description>Works transparently with existing Dapper operations</description></item>
/// </list>
/// </para>
/// <para>
/// When isolation is disabled or using a different strategy, the original connection
/// is returned without wrapping.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a Dapper store
/// public class OrderStore
/// {
///     private readonly ModuleAwareConnectionFactory _connectionFactory;
///
///     public OrderStore(ModuleAwareConnectionFactory connectionFactory)
///     {
///         _connectionFactory = connectionFactory;
///     }
///
///     public async Task&lt;Order?&gt; GetByIdAsync(Guid id, CancellationToken ct)
///     {
///         using var connection = _connectionFactory.CreateConnection();
///         await connection.OpenAsync(ct);
///         return await connection.QueryFirstOrDefaultAsync&lt;Order&gt;(
///             "SELECT * FROM orders.Orders WHERE Id = @Id", new { Id = id });
///     }
/// }
/// </code>
/// </example>
public sealed class ModuleAwareConnectionFactory
{
    private readonly Func<IDbConnection> _innerFactory;
    private readonly IModuleExecutionContext _moduleContext;
    private readonly IModuleSchemaRegistry _schemaRegistry;
    private readonly ModuleIsolationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleAwareConnectionFactory"/> class.
    /// </summary>
    /// <param name="innerFactory">The underlying connection factory.</param>
    /// <param name="moduleContext">The module execution context.</param>
    /// <param name="schemaRegistry">The schema registry for validation.</param>
    /// <param name="options">The module isolation options.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public ModuleAwareConnectionFactory(
        Func<IDbConnection> innerFactory,
        IModuleExecutionContext moduleContext,
        IModuleSchemaRegistry schemaRegistry,
        ModuleIsolationOptions options)
    {
        _innerFactory = innerFactory ?? throw new ArgumentNullException(nameof(innerFactory));
        _moduleContext = moduleContext ?? throw new ArgumentNullException(nameof(moduleContext));
        _schemaRegistry = schemaRegistry ?? throw new ArgumentNullException(nameof(schemaRegistry));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Creates a new database connection with optional schema validation.
    /// </summary>
    /// <returns>
    /// A <see cref="SchemaValidatingConnection"/> if validation is enabled,
    /// otherwise the original connection from the inner factory.
    /// </returns>
    /// <remarks>
    /// <para>
    /// When <see cref="ModuleIsolationOptions.Strategy"/> is
    /// <see cref="ModuleIsolationStrategy.DevelopmentValidationOnly"/>,
    /// the returned connection will validate SQL statements before execution.
    /// </para>
    /// <para>
    /// The connection must be opened before use, and should be disposed after use.
    /// </para>
    /// </remarks>
    public IDbConnection CreateConnection()
    {
        var innerConnection = _innerFactory();

        // Only wrap if validation is enabled and the connection is a DbConnection
        if (_options.Strategy == ModuleIsolationStrategy.DevelopmentValidationOnly &&
            innerConnection is DbConnection dbConnection)
        {
            return new SchemaValidatingConnection(
                dbConnection,
                _moduleContext,
                _schemaRegistry,
                _options);
        }

        return innerConnection;
    }

    /// <summary>
    /// Creates a new database connection with optional schema validation.
    /// </summary>
    /// <returns>
    /// A <see cref="DbConnection"/> - either a <see cref="SchemaValidatingConnection"/>
    /// if validation is enabled, otherwise the original connection cast to DbConnection.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the inner factory returns a connection that is not a <see cref="DbConnection"/>.
    /// </exception>
    /// <remarks>
    /// Use this method when you need a <see cref="DbConnection"/> specifically
    /// (e.g., for async operations that require DbConnection).
    /// </remarks>
    public DbConnection CreateDbConnection()
    {
        var connection = CreateConnection();

        if (connection is DbConnection dbConnection)
        {
            return dbConnection;
        }

        throw new InvalidOperationException(
            $"The inner connection factory returned a connection of type '{connection.GetType().FullName}' " +
            $"which is not a DbConnection. Use CreateConnection() instead or ensure the factory returns a DbConnection.");
    }
}
