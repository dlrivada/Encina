using System.Data;
using System.Data.Common;
using Encina.Modules.Isolation;

namespace Encina.ADO.Oracle.Modules;

/// <summary>
/// A connection factory that creates module-aware database connections for Oracle.
/// </summary>
/// <remarks>
/// <para>
/// This factory wraps database connections with schema validation when module isolation
/// is enabled in development mode. It is used to enforce module boundaries at the ADO.NET level.
/// </para>
/// <para>
/// <b>Behavior by Strategy</b>:
/// <list type="bullet">
/// <item><description>
/// <see cref="ModuleIsolationStrategy.DevelopmentValidationOnly"/>: Wraps connections with
/// <see cref="SchemaValidatingConnection"/> that validates SQL against module schema boundaries.
/// </description></item>
/// <item><description>
/// Other strategies: Returns the underlying connection without wrapping.
/// </description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ModuleAwareConnectionFactory
{
    private readonly Func<IDbConnection> _innerConnectionFactory;
    private readonly IModuleExecutionContext _moduleContext;
    private readonly IModuleSchemaRegistry _schemaRegistry;
    private readonly ModuleIsolationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleAwareConnectionFactory"/> class.
    /// </summary>
    /// <param name="innerConnectionFactory">The factory function to create underlying connections.</param>
    /// <param name="moduleContext">The module execution context.</param>
    /// <param name="schemaRegistry">The schema registry for validation.</param>
    /// <param name="options">The module isolation options.</param>
    public ModuleAwareConnectionFactory(
        Func<IDbConnection> innerConnectionFactory,
        IModuleExecutionContext moduleContext,
        IModuleSchemaRegistry schemaRegistry,
        ModuleIsolationOptions options)
    {
        _innerConnectionFactory = innerConnectionFactory ?? throw new ArgumentNullException(nameof(innerConnectionFactory));
        _moduleContext = moduleContext ?? throw new ArgumentNullException(nameof(moduleContext));
        _schemaRegistry = schemaRegistry ?? throw new ArgumentNullException(nameof(schemaRegistry));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Creates a module-aware database connection.
    /// </summary>
    /// <returns>
    /// A database connection that validates SQL against module schema boundaries when
    /// module isolation is enabled in development mode; otherwise, the underlying connection.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the underlying connection is not a <see cref="DbConnection"/>.
    /// </exception>
    public IDbConnection CreateConnection()
    {
        var connection = _innerConnectionFactory();

        // Only wrap in DevelopmentValidationOnly mode
        if (_options.Strategy == ModuleIsolationStrategy.DevelopmentValidationOnly)
        {
            if (connection is not DbConnection dbConnection)
            {
                throw new InvalidOperationException(
                    $"Module isolation requires a DbConnection, but got {connection.GetType().Name}. " +
                    "Ensure your connection factory returns a DbConnection-derived type.");
            }

            return new SchemaValidatingConnection(
                dbConnection,
                _moduleContext,
                _schemaRegistry,
                _options);
        }

        return connection;
    }
}
