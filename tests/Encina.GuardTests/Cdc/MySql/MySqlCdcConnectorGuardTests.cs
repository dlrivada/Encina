using System.Reflection;
using Encina.Cdc.Abstractions;
using Encina.Cdc.MySql;

namespace Encina.GuardTests.Cdc.MySql;

/// <summary>
/// Guard clause tests for the internal <c>MySqlCdcConnector</c> and <c>MySqlBinlogStatusClient</c>
/// constructors. The package does not expose internals to this project, so the real constructors
/// are invoked through reflection on the types of the production assembly.
/// </summary>
public sealed class MySqlCdcConnectorGuardTests
{
    private static readonly Assembly Package = typeof(MySqlCdcOptions).Assembly;

    private static ArgumentNullException? Construct(string typeName, params object?[] args)
    {
        var type = Package.GetType(typeName, throwOnError: true)!;
        try
        {
            Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public, null, args, null);
            return null;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentNullException ane)
        {
            return ane;
        }
    }

    private static object ValidClient() =>
        Activator.CreateInstance(
            Package.GetType("Encina.Cdc.MySql.MySqlBinlogStatusClient", true)!,
            new MySqlCdcOptions())!;

    private static object?[] ConnectorArgs(
        object? options,
        object? store,
        object? logger,
        object? client) =>
        [options, store, logger, client, null];

    private static object?[] Valid() =>
        ConnectorArgs(
            new MySqlCdcOptions(),
            Substitute.For<ICdcPositionStore>(),
            CreateLogger(),
            ValidClient());

    private static object CreateLogger() =>
        Activator.CreateInstance(
            typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>).MakeGenericType(
                Package.GetType("Encina.Cdc.MySql.MySqlCdcConnector", true)!))!;

    private const string ConnectorType = "Encina.Cdc.MySql.MySqlCdcConnector";

    [Fact]
    public void Connector_NullOptions_ShouldThrow()
    {
        var args = Valid();
        args[0] = null;
        Construct(ConnectorType, args)!.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Connector_NullPositionStore_ShouldThrow()
    {
        var args = Valid();
        args[1] = null;
        Construct(ConnectorType, args)!.ParamName.ShouldBe("positionStore");
    }

    [Fact]
    public void Connector_NullLogger_ShouldThrow()
    {
        var args = Valid();
        args[2] = null;
        Construct(ConnectorType, args)!.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Connector_NullBinlogStatusClient_ShouldThrow()
    {
        var args = Valid();
        args[3] = null;
        Construct(ConnectorType, args)!.ParamName.ShouldBe("binlogStatusClient");
    }

    [Fact]
    public void Connector_AllArgumentsValid_ShouldNotThrow()
    {
        Construct(ConnectorType, Valid()).ShouldBeNull();
    }

    [Fact]
    public void BinlogStatusClient_NullOptions_ShouldThrow()
    {
        Construct("Encina.Cdc.MySql.MySqlBinlogStatusClient", [null])!.ParamName.ShouldBe("options");
    }
}
