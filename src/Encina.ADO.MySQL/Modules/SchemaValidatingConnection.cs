using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Encina.Modules.Isolation;

namespace Encina.ADO.MySQL.Modules;

/// <summary>
/// A database connection wrapper that creates schema-validating commands for MySQL.
/// </summary>
/// <remarks>
/// <para>
/// This wrapper wraps a database connection to intercept command creation.
/// When CreateCommand is called, it returns a <see cref="SchemaValidatingCommand"/>
/// that validates SQL statements against module schema boundaries before execution.
/// </para>
/// <para>
/// All other connection operations are delegated directly to the underlying connection.
/// </para>
/// <para>
/// This class is used internally by <see cref="ModuleAwareConnectionFactory"/> when
/// module isolation is enabled.
/// </para>
/// </remarks>
public sealed class SchemaValidatingConnection : DbConnection
{
    private readonly DbConnection _innerConnection;
    private readonly IModuleExecutionContext _moduleContext;
    private readonly IModuleSchemaRegistry _schemaRegistry;
    private readonly ModuleIsolationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaValidatingConnection"/> class.
    /// </summary>
    /// <param name="innerConnection">The underlying connection to wrap.</param>
    /// <param name="moduleContext">The module execution context.</param>
    /// <param name="schemaRegistry">The schema registry for validation.</param>
    /// <param name="options">The module isolation options.</param>
    public SchemaValidatingConnection(
        DbConnection innerConnection,
        IModuleExecutionContext moduleContext,
        IModuleSchemaRegistry schemaRegistry,
        ModuleIsolationOptions options)
    {
        _innerConnection = innerConnection ?? throw new ArgumentNullException(nameof(innerConnection));
        _moduleContext = moduleContext ?? throw new ArgumentNullException(nameof(moduleContext));
        _schemaRegistry = schemaRegistry ?? throw new ArgumentNullException(nameof(schemaRegistry));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    [AllowNull]
    public override string ConnectionString
    {
        get => _innerConnection.ConnectionString;
        set => _innerConnection.ConnectionString = value!;
    }

    /// <inheritdoc/>
    public override string Database => _innerConnection.Database;

    /// <inheritdoc/>
    public override string DataSource => _innerConnection.DataSource;

    /// <inheritdoc/>
    public override string ServerVersion => _innerConnection.ServerVersion;

    /// <inheritdoc/>
    public override ConnectionState State => _innerConnection.State;

    /// <inheritdoc/>
    public override void ChangeDatabase(string databaseName) => _innerConnection.ChangeDatabase(databaseName);

    /// <inheritdoc/>
    public override void Close() => _innerConnection.Close();

    /// <inheritdoc/>
    public override Task CloseAsync() => _innerConnection.CloseAsync();

    /// <inheritdoc/>
    public override void Open() => _innerConnection.Open();

    /// <inheritdoc/>
    public override Task OpenAsync(CancellationToken cancellationToken) =>
        _innerConnection.OpenAsync(cancellationToken);

    /// <inheritdoc/>
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        _innerConnection.BeginTransaction(isolationLevel);

    /// <inheritdoc/>
    protected override async ValueTask<DbTransaction> BeginDbTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken) =>
        await _innerConnection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    protected override DbCommand CreateDbCommand()
    {
        var innerCommand = _innerConnection.CreateCommand();

        // Only wrap with validation in DevelopmentValidationOnly mode
        if (_options.Strategy == ModuleIsolationStrategy.DevelopmentValidationOnly)
        {
            return new SchemaValidatingCommand(
                innerCommand,
                _moduleContext,
                _schemaRegistry,
                _options);
        }

        return innerCommand;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerConnection.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await _innerConnection.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the underlying connection (for advanced scenarios).
    /// </summary>
    internal DbConnection InnerConnection => _innerConnection;
}
