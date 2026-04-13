using Encina.Testing.Testcontainers;

using Shouldly;

namespace Encina.UnitTests.Testing.Testcontainers;

/// <summary>
/// Unit tests for <see cref="EncinaContainers"/> factory methods.
/// These tests exercise factory creation without starting Docker containers.
/// </summary>
[Trait("Category", "Unit")]
public sealed class EncinaContainersTests
{
    // ─── Default factory methods ───

    [Fact]
    public void SqlServer_Default_CreatesFixture()
    {
        var fixture = EncinaContainers.SqlServer();
        fixture.ShouldNotBeNull();
        fixture.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void PostgreSql_Default_CreatesFixture()
    {
        var fixture = EncinaContainers.PostgreSql();
        fixture.ShouldNotBeNull();
        fixture.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void MySql_Default_CreatesFixture()
    {
        var fixture = EncinaContainers.MySql();
        fixture.ShouldNotBeNull();
        fixture.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void MongoDb_Default_CreatesFixture()
    {
        var fixture = EncinaContainers.MongoDb();
        fixture.ShouldNotBeNull();
        fixture.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void Redis_Default_CreatesFixture()
    {
        var fixture = EncinaContainers.Redis();
        fixture.ShouldNotBeNull();
        fixture.IsRunning.ShouldBeFalse();
    }

    // ─── Custom factory methods ───

    [Fact]
    public void SqlServer_WithConfigure_CreatesConfiguredFixture()
    {
        var fixture = EncinaContainers.SqlServer(_ => { });
        fixture.ShouldNotBeNull();
        fixture.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void PostgreSql_WithConfigure_CreatesConfiguredFixture()
    {
        var fixture = EncinaContainers.PostgreSql(_ => { });
        fixture.ShouldNotBeNull();
        fixture.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void MySql_WithConfigure_CreatesConfiguredFixture()
    {
        var fixture = EncinaContainers.MySql(_ => { });
        fixture.ShouldNotBeNull();
        fixture.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void MongoDb_WithConfigure_CreatesConfiguredFixture()
    {
        var fixture = EncinaContainers.MongoDb(_ => { });
        fixture.ShouldNotBeNull();
        fixture.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void Redis_WithConfigure_CreatesConfiguredFixture()
    {
        var fixture = EncinaContainers.Redis(_ => { });
        fixture.ShouldNotBeNull();
        fixture.IsRunning.ShouldBeFalse();
    }

    // ─── Null configure guards ───

    [Fact]
    public void SqlServer_NullConfigure_Throws()
    {
        Should.Throw<ArgumentNullException>(() => EncinaContainers.SqlServer(null!));
    }

    [Fact]
    public void PostgreSql_NullConfigure_Throws()
    {
        Should.Throw<ArgumentNullException>(() => EncinaContainers.PostgreSql(null!));
    }

    [Fact]
    public void MySql_NullConfigure_Throws()
    {
        Should.Throw<ArgumentNullException>(() => EncinaContainers.MySql(null!));
    }

    [Fact]
    public void MongoDb_NullConfigure_Throws()
    {
        Should.Throw<ArgumentNullException>(() => EncinaContainers.MongoDb(null!));
    }

    [Fact]
    public void Redis_NullConfigure_Throws()
    {
        Should.Throw<ArgumentNullException>(() => EncinaContainers.Redis(null!));
    }
}
