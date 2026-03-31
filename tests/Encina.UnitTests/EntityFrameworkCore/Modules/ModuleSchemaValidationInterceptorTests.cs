using System.Data.Common;
using Encina.EntityFrameworkCore.Modules;
using Encina.Modules.Isolation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Encina.UnitTests.EntityFrameworkCore.Modules;

/// <summary>
/// Unit tests for <see cref="ModuleSchemaValidationInterceptor"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ModuleSchemaValidationInterceptorTests
{
    private readonly IModuleExecutionContext _moduleContext;
    private readonly IModuleSchemaRegistry _schemaRegistry;
    private readonly ModuleIsolationOptions _options;
    private readonly ILogger<ModuleSchemaValidationInterceptor> _logger;

    public ModuleSchemaValidationInterceptorTests()
    {
        _moduleContext = Substitute.For<IModuleExecutionContext>();
        _schemaRegistry = Substitute.For<IModuleSchemaRegistry>();
        _options = new ModuleIsolationOptions();
        _logger = Substitute.For<ILogger<ModuleSchemaValidationInterceptor>>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullModuleContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ModuleSchemaValidationInterceptor(null!, _schemaRegistry, _options, _logger));
        ex.ParamName.ShouldBe("moduleContext");
    }

    [Fact]
    public void Constructor_NullSchemaRegistry_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ModuleSchemaValidationInterceptor(_moduleContext, null!, _options, _logger));
        ex.ParamName.ShouldBe("schemaRegistry");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ModuleSchemaValidationInterceptor(_moduleContext, _schemaRegistry, null!, _logger));
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ModuleSchemaValidationInterceptor(_moduleContext, _schemaRegistry, _options, null!));
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Act
        var interceptor = CreateInterceptor();

        // Assert
        interceptor.ShouldNotBeNull();
    }

    #endregion

    #region Strategy Skip Tests

    [Fact]
    public void ReaderExecuting_NonDevelopmentStrategy_SkipsValidation()
    {
        // Arrange
        _options.Strategy = ModuleIsolationStrategy.SchemaWithPermissions;
        var interceptor = CreateInterceptor();
        var command = CreateDbCommand("SELECT * FROM orders.Products");
        var eventData = CreateCommandEventData();

        // Act
        var result = interceptor.ReaderExecuting(command, eventData, default);

        // Assert - should not call schema registry at all
        _schemaRegistry.DidNotReceive().ValidateSqlAccess(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void NonQueryExecuting_ConnectionPerModuleStrategy_SkipsValidation()
    {
        // Arrange
        _options.Strategy = ModuleIsolationStrategy.ConnectionPerModule;
        var interceptor = CreateInterceptor();
        var command = CreateDbCommand("INSERT INTO orders.Products VALUES (1)");
        var eventData = CreateCommandEventData();

        // Act
        var result = interceptor.NonQueryExecuting(command, eventData, default);

        // Assert
        _schemaRegistry.DidNotReceive().ValidateSqlAccess(Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion

    #region No Module Context Tests

    [Fact]
    public void ReaderExecuting_NoModuleContext_SkipsValidation()
    {
        // Arrange
        _moduleContext.CurrentModule.Returns((string?)null);
        var interceptor = CreateInterceptor();
        var command = CreateDbCommand("SELECT * FROM orders.Products");
        var eventData = CreateCommandEventData();

        // Act
        var result = interceptor.ReaderExecuting(command, eventData, default);

        // Assert
        _schemaRegistry.DidNotReceive().ValidateSqlAccess(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void ReaderExecuting_EmptyModuleContext_SkipsValidation()
    {
        // Arrange
        _moduleContext.CurrentModule.Returns("");
        var interceptor = CreateInterceptor();
        var command = CreateDbCommand("SELECT * FROM orders.Products");
        var eventData = CreateCommandEventData();

        // Act
        var result = interceptor.ReaderExecuting(command, eventData, default);

        // Assert
        _schemaRegistry.DidNotReceive().ValidateSqlAccess(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void ReaderExecuting_WhitespaceModuleContext_SkipsValidation()
    {
        // Arrange
        _moduleContext.CurrentModule.Returns("   ");
        var interceptor = CreateInterceptor();
        var command = CreateDbCommand("SELECT * FROM orders.Products");
        var eventData = CreateCommandEventData();

        // Act
        var result = interceptor.ReaderExecuting(command, eventData, default);

        // Assert
        _schemaRegistry.DidNotReceive().ValidateSqlAccess(Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion

    #region Empty SQL Tests

    [Fact]
    public void ReaderExecuting_EmptySql_SkipsValidation()
    {
        // Arrange
        _moduleContext.CurrentModule.Returns("Orders");
        var interceptor = CreateInterceptor();
        var command = CreateDbCommand("");
        var eventData = CreateCommandEventData();

        // Act
        var result = interceptor.ReaderExecuting(command, eventData, default);

        // Assert
        _schemaRegistry.DidNotReceive().ValidateSqlAccess(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void ReaderExecuting_NullSql_SkipsValidation()
    {
        // Arrange
        _moduleContext.CurrentModule.Returns("Orders");
        var interceptor = CreateInterceptor();
        var command = CreateDbCommand(null!);
        var eventData = CreateCommandEventData();

        // Act
        var result = interceptor.ReaderExecuting(command, eventData, default);

        // Assert
        _schemaRegistry.DidNotReceive().ValidateSqlAccess(Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion

    #region Validation Success Tests

    [Fact]
    public void ReaderExecuting_ValidSchemaAccess_DoesNotThrow()
    {
        // Arrange
        _moduleContext.CurrentModule.Returns("Orders");
        var accessedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "orders" };
        var allowedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "orders", "shared" };
        _schemaRegistry.ValidateSqlAccess("Orders", Arg.Any<string>())
            .Returns(SchemaAccessValidationResult.Success(accessedSchemas, allowedSchemas));

        var interceptor = CreateInterceptor();
        var command = CreateDbCommand("SELECT * FROM orders.Products");
        var eventData = CreateCommandEventData();

        // Act & Assert
        Should.NotThrow(() => interceptor.ReaderExecuting(command, eventData, default));
    }

    [Fact]
    public void ReaderExecuting_ValidSchemaAccess_NoAccessedSchemas_DoesNotThrow()
    {
        // Arrange
        _moduleContext.CurrentModule.Returns("Orders");
        var accessedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allowedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "orders" };
        _schemaRegistry.ValidateSqlAccess("Orders", Arg.Any<string>())
            .Returns(SchemaAccessValidationResult.Success(accessedSchemas, allowedSchemas));

        var interceptor = CreateInterceptor();
        var command = CreateDbCommand("SELECT 1");
        var eventData = CreateCommandEventData();

        // Act & Assert
        Should.NotThrow(() => interceptor.ReaderExecuting(command, eventData, default));
    }

    #endregion

    #region Validation Failure Tests

    [Fact]
    public void ReaderExecuting_UnauthorizedSchemaAccess_ThrowsModuleIsolationViolationException()
    {
        // Arrange
        _moduleContext.CurrentModule.Returns("Orders");
        var accessedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "orders", "payments" };
        var unauthorized = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "payments" };
        var allowedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "orders", "shared" };
        _schemaRegistry.ValidateSqlAccess("Orders", Arg.Any<string>())
            .Returns(SchemaAccessValidationResult.Failure(accessedSchemas, unauthorized, allowedSchemas));

        var interceptor = CreateInterceptor();
        var command = CreateDbCommand("SELECT * FROM payments.Invoices");
        var eventData = CreateCommandEventData();

        // Act & Assert
        var ex = Should.Throw<ModuleIsolationViolationException>(() =>
            interceptor.ReaderExecuting(command, eventData, default));
        ex.ModuleName.ShouldBe("Orders");
    }

    [Fact]
    public void NonQueryExecuting_UnauthorizedSchemaAccess_ThrowsModuleIsolationViolationException()
    {
        // Arrange
        _moduleContext.CurrentModule.Returns("Orders");
        var accessedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "payments" };
        var unauthorized = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "payments" };
        var allowedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "orders" };
        _schemaRegistry.ValidateSqlAccess("Orders", Arg.Any<string>())
            .Returns(SchemaAccessValidationResult.Failure(accessedSchemas, unauthorized, allowedSchemas));

        var interceptor = CreateInterceptor();
        var command = CreateDbCommand("INSERT INTO payments.Invoices VALUES (1)");
        var eventData = CreateCommandEventData();

        // Act & Assert
        Should.Throw<ModuleIsolationViolationException>(() =>
            interceptor.NonQueryExecuting(command, eventData, default));
    }

    [Fact]
    public void ScalarExecuting_UnauthorizedSchemaAccess_ThrowsModuleIsolationViolationException()
    {
        // Arrange
        _moduleContext.CurrentModule.Returns("Orders");
        var accessedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "payments" };
        var unauthorized = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "payments" };
        var allowedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "orders" };
        _schemaRegistry.ValidateSqlAccess("Orders", Arg.Any<string>())
            .Returns(SchemaAccessValidationResult.Failure(accessedSchemas, unauthorized, allowedSchemas));

        var interceptor = CreateInterceptor();
        var command = CreateDbCommand("SELECT COUNT(*) FROM payments.Invoices");
        var eventData = CreateCommandEventData();

        // Act & Assert
        Should.Throw<ModuleIsolationViolationException>(() =>
            interceptor.ScalarExecuting(command, eventData, default));
    }

    #endregion

    #region Async Overload Tests

    [Fact]
    public async Task ReaderExecutingAsync_ValidAccess_DoesNotThrow()
    {
        // Arrange
        _moduleContext.CurrentModule.Returns("Orders");
        var accessedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "orders" };
        var allowedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "orders" };
        _schemaRegistry.ValidateSqlAccess("Orders", Arg.Any<string>())
            .Returns(SchemaAccessValidationResult.Success(accessedSchemas, allowedSchemas));

        var interceptor = CreateInterceptor();
        var command = CreateDbCommand("SELECT * FROM orders.Products");
        var eventData = CreateCommandEventData();

        // Act & Assert
        await Should.NotThrowAsync(async () =>
            await interceptor.ReaderExecutingAsync(command, eventData, default));
    }

    [Fact]
    public async Task ReaderExecutingAsync_UnauthorizedAccess_Throws()
    {
        // Arrange
        _moduleContext.CurrentModule.Returns("Orders");
        var accessedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "payments" };
        var unauthorized = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "payments" };
        var allowedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "orders" };
        _schemaRegistry.ValidateSqlAccess("Orders", Arg.Any<string>())
            .Returns(SchemaAccessValidationResult.Failure(accessedSchemas, unauthorized, allowedSchemas));

        var interceptor = CreateInterceptor();
        var command = CreateDbCommand("SELECT * FROM payments.Invoices");
        var eventData = CreateCommandEventData();

        // Act & Assert
        await Should.ThrowAsync<ModuleIsolationViolationException>(async () =>
            await interceptor.ReaderExecutingAsync(command, eventData, default));
    }

    [Fact]
    public async Task NonQueryExecutingAsync_UnauthorizedAccess_Throws()
    {
        // Arrange
        _moduleContext.CurrentModule.Returns("Orders");
        var accessedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "payments" };
        var unauthorized = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "payments" };
        var allowedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "orders" };
        _schemaRegistry.ValidateSqlAccess("Orders", Arg.Any<string>())
            .Returns(SchemaAccessValidationResult.Failure(accessedSchemas, unauthorized, allowedSchemas));

        var interceptor = CreateInterceptor();
        var command = CreateDbCommand("DELETE FROM payments.Invoices");
        var eventData = CreateCommandEventData();

        // Act & Assert
        await Should.ThrowAsync<ModuleIsolationViolationException>(async () =>
            await interceptor.NonQueryExecutingAsync(command, eventData, default));
    }

    [Fact]
    public async Task ScalarExecutingAsync_UnauthorizedAccess_Throws()
    {
        // Arrange
        _moduleContext.CurrentModule.Returns("Orders");
        var accessedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "payments" };
        var unauthorized = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "payments" };
        var allowedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "orders" };
        _schemaRegistry.ValidateSqlAccess("Orders", Arg.Any<string>())
            .Returns(SchemaAccessValidationResult.Failure(accessedSchemas, unauthorized, allowedSchemas));

        var interceptor = CreateInterceptor();
        var command = CreateDbCommand("SELECT COUNT(*) FROM payments.Invoices");
        var eventData = CreateCommandEventData();

        // Act & Assert
        await Should.ThrowAsync<ModuleIsolationViolationException>(async () =>
            await interceptor.ScalarExecutingAsync(command, eventData, default));
    }

    #endregion

    #region SQL Truncation Tests

    [Fact]
    public void ReaderExecuting_LongSql_TruncatesSqlInException()
    {
        // Arrange
        _moduleContext.CurrentModule.Returns("Orders");
        var longSql = "SELECT * FROM payments.Invoices WHERE " + new string('x', 2000);
        var accessedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "payments" };
        var unauthorized = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "payments" };
        var allowedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "orders" };
        _schemaRegistry.ValidateSqlAccess("Orders", Arg.Any<string>())
            .Returns(SchemaAccessValidationResult.Failure(accessedSchemas, unauthorized, allowedSchemas));

        var interceptor = CreateInterceptor();
        var command = CreateDbCommand(longSql);
        var eventData = CreateCommandEventData();

        // Act & Assert
        var ex = Should.Throw<ModuleIsolationViolationException>(() =>
            interceptor.ReaderExecuting(command, eventData, default));
        ex.SqlStatement.ShouldNotBeNull();
        ex.SqlStatement.Length.ShouldBeLessThanOrEqualTo(1003); // 1000 + "..."
        ex.SqlStatement.ShouldEndWith("...");
    }

    [Fact]
    public void ReaderExecuting_ShortSql_DoesNotTruncateSqlInException()
    {
        // Arrange
        _moduleContext.CurrentModule.Returns("Orders");
        var shortSql = "SELECT * FROM payments.Invoices";
        var accessedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "payments" };
        var unauthorized = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "payments" };
        var allowedSchemas = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "orders" };
        _schemaRegistry.ValidateSqlAccess("Orders", Arg.Any<string>())
            .Returns(SchemaAccessValidationResult.Failure(accessedSchemas, unauthorized, allowedSchemas));

        var interceptor = CreateInterceptor();
        var command = CreateDbCommand(shortSql);
        var eventData = CreateCommandEventData();

        // Act & Assert
        var ex = Should.Throw<ModuleIsolationViolationException>(() =>
            interceptor.ReaderExecuting(command, eventData, default));
        ex.SqlStatement.ShouldBe(shortSql);
    }

    #endregion

    #region Helpers

    private ModuleSchemaValidationInterceptor CreateInterceptor()
    {
        return new ModuleSchemaValidationInterceptor(
            _moduleContext, _schemaRegistry, _options, _logger);
    }

    private static DbCommand CreateDbCommand(string commandText)
    {
        var command = Substitute.For<DbCommand>();
        command.CommandText.Returns(commandText);
        return command;
    }

    private static CommandEventData CreateCommandEventData()
    {
        var eventDefinition = Substitute.For<EventDefinitionBase>(
            Substitute.For<ILoggingOptions>(),
            new EventId(1),
            LogLevel.Debug,
            "test");

        var messageGenerator = Substitute.For<Func<EventDefinitionBase, EventData, string>>();

        return new CommandEventData(
            eventDefinition,
            messageGenerator,
            connection: Substitute.For<DbConnection>(),
            command: Substitute.For<DbCommand>(),
            logCommandText: "SELECT 1",
            context: null,
            executeMethod: DbCommandMethod.ExecuteReader,
            commandId: Guid.NewGuid(),
            connectionId: Guid.NewGuid(),
            async: false,
            logParameterValues: false,
            startTime: DateTimeOffset.UtcNow,
            commandSource: CommandSource.LinqQuery);
    }

    #endregion
}
