using System.Reflection;
using Encina.Cdc.Debezium.Health;
using Encina.Cdc.Debezium.Kafka;
using Encina.Cdc.Health;
using Encina.Cdc.MongoDb.Health;
using Encina.Cdc.MySql.Health;
using Encina.Cdc.PostgreSql.Health;
using Encina.Cdc.SqlServer.Health;

namespace Encina.ContractTests.Cdc;

/// <summary>
/// Contract tests verifying that all provider-specific CDC health checks inherit from
/// <see cref="CdcHealthCheck"/> and expose a <c>DefaultName</c> constant for registration.
/// Covers: <see cref="PostgresCdcHealthCheck"/>, <see cref="MySqlCdcHealthCheck"/>,
/// <see cref="SqlServerCdcHealthCheck"/>, <see cref="MongoCdcHealthCheck"/>,
/// <see cref="DebeziumCdcHealthCheck"/>, and <see cref="DebeziumKafkaHealthCheck"/>.
/// </summary>
[Trait("Category", "Contract")]
public sealed class CdcHealthCheckContractTests
{
    /// <summary>
    /// All CDC health check types that must satisfy the contract.
    /// </summary>
    public static TheoryData<Type, string> HealthCheckTypes => new()
    {
        { typeof(PostgresCdcHealthCheck), "encina-cdc-postgres" },
        { typeof(MySqlCdcHealthCheck), "encina-cdc-mysql" },
        { typeof(SqlServerCdcHealthCheck), "encina-cdc-sqlserver" },
        { typeof(MongoCdcHealthCheck), "encina-cdc-mongodb" },
        { typeof(DebeziumCdcHealthCheck), "encina-cdc-debezium" },
        { typeof(DebeziumKafkaHealthCheck), "encina-cdc-debezium-kafka" }
    };

    #region Inheritance Contract

    /// <summary>
    /// Contract: All provider-specific CDC health checks must inherit from <see cref="CdcHealthCheck"/>.
    /// </summary>
    [Theory]
    [MemberData(nameof(HealthCheckTypes))]
    public void Contract_AllCdcHealthChecks_InheritFromCdcHealthCheck(Type healthCheckType, string _)
    {
        healthCheckType.IsSubclassOf(typeof(CdcHealthCheck)).ShouldBeTrue(
            $"{healthCheckType.Name} must inherit from CdcHealthCheck");
    }

    #endregion

    #region DefaultName Constant Contract

    /// <summary>
    /// Contract: All provider-specific CDC health checks must define a <c>DefaultName</c> constant.
    /// </summary>
    [Theory]
    [MemberData(nameof(HealthCheckTypes))]
    public void Contract_AllCdcHealthChecks_HaveDefaultNameConstant(Type healthCheckType, string _)
    {
        // Arrange
        var field = healthCheckType.GetField("DefaultName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        // Assert
        field.ShouldNotBeNull(
            $"{healthCheckType.Name} must define a public const DefaultName field");
        field.IsLiteral.ShouldBeTrue(
            $"{healthCheckType.Name}.DefaultName must be a const string");
        field.FieldType.ShouldBe(typeof(string),
            $"{healthCheckType.Name}.DefaultName must be of type string");
    }

    /// <summary>
    /// Contract: All <c>DefaultName</c> constants must have the expected value
    /// matching the naming convention <c>encina-cdc-{provider}</c>.
    /// </summary>
    [Theory]
    [MemberData(nameof(HealthCheckTypes))]
    public void Contract_AllCdcHealthChecks_DefaultNameMatchesExpected(Type healthCheckType, string expectedName)
    {
        // Arrange
        var field = healthCheckType.GetField("DefaultName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)!;

        // Act
        var value = (string?)field.GetRawConstantValue();

        // Assert
        value.ShouldBe(expectedName,
            $"{healthCheckType.Name}.DefaultName must be '{expectedName}'");
    }

    /// <summary>
    /// Contract: All <c>DefaultName</c> values must start with the <c>encina-cdc-</c> prefix
    /// for consistent health check naming across providers.
    /// </summary>
    [Theory]
    [MemberData(nameof(HealthCheckTypes))]
    public void Contract_AllCdcHealthChecks_DefaultNameStartsWithPrefix(Type healthCheckType, string _)
    {
        // Arrange
        var field = healthCheckType.GetField("DefaultName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)!;
        var value = (string)field.GetRawConstantValue()!;

        // Assert
        value.ShouldStartWith("encina-cdc-");
    }

    #endregion

    #region DefaultName Uniqueness Contract

    /// <summary>
    /// Contract: All <c>DefaultName</c> constants must be unique across providers
    /// to prevent health check registration conflicts.
    /// </summary>
    [Fact]
    public void Contract_AllCdcHealthChecks_DefaultNames_AreUnique()
    {
        // Arrange
        var types = new[]
        {
            typeof(PostgresCdcHealthCheck),
            typeof(MySqlCdcHealthCheck),
            typeof(SqlServerCdcHealthCheck),
            typeof(MongoCdcHealthCheck),
            typeof(DebeziumCdcHealthCheck),
            typeof(DebeziumKafkaHealthCheck)
        };

        // Act
        var names = types
            .Select(t => t.GetField("DefaultName", BindingFlags.Public | BindingFlags.Static)!)
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        // Assert
        names.Distinct(StringComparer.Ordinal).Count().ShouldBe(names.Count,
            "All CDC health check DefaultName constants must be unique to prevent registration conflicts");
    }

    #endregion

    #region Base Class Contract

    /// <summary>
    /// Contract: <see cref="CdcHealthCheck"/> must have a protected constructor
    /// (not public) to enforce inheritance.
    /// </summary>
    [Fact]
    public void Contract_CdcHealthCheck_HasProtectedConstructor()
    {
        // Arrange
        var publicConstructors = typeof(CdcHealthCheck).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var protectedConstructors = typeof(CdcHealthCheck).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);

        // Assert
        publicConstructors.Length.ShouldBe(0,
            "CdcHealthCheck must not have public constructors (use protected for inheritance)");
        protectedConstructors.Length.ShouldBeGreaterThan(0,
            "CdcHealthCheck must have at least one protected constructor for subclassing");
    }

    #endregion
}
