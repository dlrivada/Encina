using System.Data.Common;
using Encina.Modules.Isolation;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Encina.EntityFrameworkCore.Modules;

/// <summary>
/// EF Core interceptor that validates SQL statements against module schema boundaries.
/// </summary>
/// <remarks>
/// <para>
/// This interceptor is the core enforcement mechanism for module isolation in development mode.
/// It intercepts all SQL commands before execution and validates that they only access
/// schemas allowed for the current module.
/// </para>
/// <para>
/// <b>When Active</b>: Only when <see cref="ModuleIsolationOptions.Strategy"/> is set to
/// <see cref="ModuleIsolationStrategy.DevelopmentValidationOnly"/>.
/// </para>
/// <para>
/// <b>Validation Process</b>:
/// <list type="number">
/// <item><description>Get the current module from <see cref="IModuleExecutionContext"/></description></item>
/// <item><description>Extract schemas from the SQL using <see cref="SqlSchemaExtractor"/></description></item>
/// <item><description>Validate against allowed schemas from <see cref="IModuleSchemaRegistry"/></description></item>
/// <item><description>Throw <see cref="ModuleIsolationViolationException"/> if unauthorized schemas are accessed</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Performance Considerations</b>:
/// <list type="bullet">
/// <item><description>Schema extraction uses compiled regex patterns for performance</description></item>
/// <item><description>Validation is skipped when no module context is set</description></item>
/// <item><description>Results are not cached as SQL statements vary per query</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // The interceptor is registered automatically when UseModuleIsolation is enabled
/// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
/// {
///     config.UseModuleIsolation = true;
///     config.ModuleIsolationOptions.Strategy = ModuleIsolationStrategy.DevelopmentValidationOnly;
/// });
///
/// // When executing a query in module context:
/// moduleExecutionContext.SetCurrentModule("Orders");
/// await dbContext.Orders.ToListAsync(); // Only orders.* tables allowed
/// </code>
/// </example>
public sealed class ModuleSchemaValidationInterceptor : DbCommandInterceptor
{
    private readonly IModuleExecutionContext _moduleContext;
    private readonly IModuleSchemaRegistry _schemaRegistry;
    private readonly ModuleIsolationOptions _options;
    private readonly ILogger<ModuleSchemaValidationInterceptor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleSchemaValidationInterceptor"/> class.
    /// </summary>
    /// <param name="moduleContext">The module execution context for determining the current module.</param>
    /// <param name="schemaRegistry">The schema registry for validation.</param>
    /// <param name="options">The module isolation options.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public ModuleSchemaValidationInterceptor(
        IModuleExecutionContext moduleContext,
        IModuleSchemaRegistry schemaRegistry,
        ModuleIsolationOptions options,
        ILogger<ModuleSchemaValidationInterceptor> logger)
    {
        ArgumentNullException.ThrowIfNull(moduleContext);
        ArgumentNullException.ThrowIfNull(schemaRegistry);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _moduleContext = moduleContext;
        _schemaRegistry = schemaRegistry;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        ValidateSchemaAccess(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        ValidateSchemaAccess(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        ValidateSchemaAccess(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ValidateSchemaAccess(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        ValidateSchemaAccess(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        ValidateSchemaAccess(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <summary>
    /// Validates that the SQL command only accesses schemas allowed for the current module.
    /// </summary>
    /// <param name="command">The database command to validate.</param>
    /// <exception cref="ModuleIsolationViolationException">
    /// Thrown when the command accesses schemas not allowed for the current module.
    /// </exception>
    private void ValidateSchemaAccess(DbCommand command)
    {
        // Only validate in DevelopmentValidationOnly mode
        if (_options.Strategy != ModuleIsolationStrategy.DevelopmentValidationOnly)
        {
            return;
        }

        // Skip if no module context is set (e.g., infrastructure queries)
        var moduleName = _moduleContext.CurrentModule;
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            return;
        }

        var sql = command.CommandText;
        if (string.IsNullOrWhiteSpace(sql))
        {
            return;
        }

        var validationResult = _schemaRegistry.ValidateSqlAccess(moduleName, sql);

        if (!validationResult.IsValid)
        {
            Log.ModuleIsolationViolationDetected(
                _logger,
                moduleName,
                string.Join(", ", validationResult.UnauthorizedSchemas),
                string.Join(", ", validationResult.AllowedSchemas));

            // Include SQL in exception for debugging (truncate if too long)
            var sqlForException = sql.Length > 1000 ? sql[..1000] + "..." : sql;

            throw new ModuleIsolationViolationException(
                moduleName,
                validationResult,
                sqlForException);
        }

        if (validationResult.AccessedSchemas.Count > 0)
        {
            Log.ModuleSchemaAccessValidated(
                _logger,
                moduleName,
                string.Join(", ", validationResult.AccessedSchemas));
        }
    }
}
