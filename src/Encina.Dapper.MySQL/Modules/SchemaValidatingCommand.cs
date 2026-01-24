using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Encina.Modules.Isolation;

namespace Encina.Dapper.MySQL.Modules;

/// <summary>
/// A database command wrapper that validates SQL statements against module schema boundaries for MySQL.
/// </summary>
/// <remarks>
/// <para>
/// This wrapper intercepts command execution methods to validate that SQL statements
/// only access schemas allowed for the current module. It is used in conjunction with
/// <see cref="SchemaValidatingConnection"/> to enforce module isolation at the Dapper level.
/// </para>
/// <para>
/// <b>Validated Methods</b>:
/// <list type="bullet">
/// <item><description>ExecuteNonQuery and ExecuteNonQueryAsync</description></item>
/// <item><description>ExecuteReader and ExecuteReaderAsync</description></item>
/// <item><description>ExecuteScalar and ExecuteScalarAsync</description></item>
/// </list>
/// </para>
/// <para>
/// Validation is only performed when the isolation strategy is set to
/// <see cref="ModuleIsolationStrategy.DevelopmentValidationOnly"/>.
/// </para>
/// </remarks>
internal sealed class SchemaValidatingCommand : DbCommand
{
    private readonly DbCommand _innerCommand;
    private readonly IModuleExecutionContext _moduleContext;
    private readonly IModuleSchemaRegistry _schemaRegistry;
    private readonly ModuleIsolationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaValidatingCommand"/> class.
    /// </summary>
    /// <param name="innerCommand">The underlying command to wrap.</param>
    /// <param name="moduleContext">The module execution context.</param>
    /// <param name="schemaRegistry">The schema registry for validation.</param>
    /// <param name="options">The module isolation options.</param>
    public SchemaValidatingCommand(
        DbCommand innerCommand,
        IModuleExecutionContext moduleContext,
        IModuleSchemaRegistry schemaRegistry,
        ModuleIsolationOptions options)
    {
        _innerCommand = innerCommand ?? throw new ArgumentNullException(nameof(innerCommand));
        _moduleContext = moduleContext ?? throw new ArgumentNullException(nameof(moduleContext));
        _schemaRegistry = schemaRegistry ?? throw new ArgumentNullException(nameof(schemaRegistry));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    [AllowNull]
    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "This is a pass-through wrapper; the SQL is validated for schema access, not security. Security validation is the responsibility of the calling code.")]
    public override string CommandText
    {
        get => _innerCommand.CommandText;
        set => _innerCommand.CommandText = value!;
    }

    /// <inheritdoc/>
    public override int CommandTimeout
    {
        get => _innerCommand.CommandTimeout;
        set => _innerCommand.CommandTimeout = value;
    }

    /// <inheritdoc/>
    public override CommandType CommandType
    {
        get => _innerCommand.CommandType;
        set => _innerCommand.CommandType = value;
    }

    /// <inheritdoc/>
    public override bool DesignTimeVisible
    {
        get => _innerCommand.DesignTimeVisible;
        set => _innerCommand.DesignTimeVisible = value;
    }

    /// <inheritdoc/>
    public override UpdateRowSource UpdatedRowSource
    {
        get => _innerCommand.UpdatedRowSource;
        set => _innerCommand.UpdatedRowSource = value;
    }

    /// <inheritdoc/>
    protected override DbConnection? DbConnection
    {
        get => _innerCommand.Connection;
        set => _innerCommand.Connection = value;
    }

    /// <inheritdoc/>
    protected override DbParameterCollection DbParameterCollection => _innerCommand.Parameters;

    /// <inheritdoc/>
    protected override DbTransaction? DbTransaction
    {
        get => _innerCommand.Transaction;
        set => _innerCommand.Transaction = value;
    }

    /// <inheritdoc/>
    public override void Cancel() => _innerCommand.Cancel();

    /// <inheritdoc/>
    public override int ExecuteNonQuery()
    {
        ValidateSchemaAccess();
        return _innerCommand.ExecuteNonQuery();
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        ValidateSchemaAccess();
        return await _innerCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override object? ExecuteScalar()
    {
        ValidateSchemaAccess();
        return _innerCommand.ExecuteScalar();
    }

    /// <inheritdoc/>
    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        ValidateSchemaAccess();
        return await _innerCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override void Prepare() => _innerCommand.Prepare();

    /// <inheritdoc/>
    protected override DbParameter CreateDbParameter() => _innerCommand.CreateParameter();

    /// <inheritdoc/>
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        ValidateSchemaAccess();
        return _innerCommand.ExecuteReader(behavior);
    }

    /// <inheritdoc/>
    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
        CommandBehavior behavior,
        CancellationToken cancellationToken)
    {
        ValidateSchemaAccess();
        return await _innerCommand.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerCommand.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await _innerCommand.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Validates that the command text only accesses schemas allowed for the current module.
    /// </summary>
    /// <exception cref="ModuleIsolationViolationException">
    /// Thrown when the command accesses schemas not allowed for the current module.
    /// </exception>
    private void ValidateSchemaAccess()
    {
        // Only validate in DevelopmentValidationOnly mode
        if (_options.Strategy != ModuleIsolationStrategy.DevelopmentValidationOnly)
        {
            return;
        }

        // Skip if no module context is set
        var moduleName = _moduleContext.CurrentModule;
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            return;
        }

        var sql = CommandText;
        if (string.IsNullOrWhiteSpace(sql))
        {
            return;
        }

        var validationResult = _schemaRegistry.ValidateSqlAccess(moduleName, sql);

        if (!validationResult.IsValid)
        {
            // Include SQL in exception for debugging (truncate if too long)
            var sqlForException = sql.Length > 1000 ? sql[..1000] + "..." : sql;

            throw new ModuleIsolationViolationException(
                moduleName,
                validationResult,
                sqlForException);
        }
    }
}
