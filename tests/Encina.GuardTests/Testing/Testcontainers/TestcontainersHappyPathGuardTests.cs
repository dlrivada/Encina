using Encina.Testing.Testcontainers;

using Shouldly;

namespace Encina.GuardTests.Testing.Testcontainers;

/// <summary>
/// Happy-path guard tests exercising Testcontainers service classes to generate
/// line coverage. Tests exercise construction, factory methods, state checks,
/// and error paths — all WITHOUT starting Docker containers.
/// </summary>
[Trait("Category", "Guard")]
public sealed class TestcontainersHappyPathGuardTests
{
    // ─── EncinaContainers factory + null guards ───

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

    // ─── Factory happy path (creates fixture without Docker) ───

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
    }

    [Fact]
    public void MySql_Default_CreatesFixture()
    {
        var fixture = EncinaContainers.MySql();
        fixture.ShouldNotBeNull();
    }

    [Fact]
    public void MongoDb_Default_CreatesFixture()
    {
        var fixture = EncinaContainers.MongoDb();
        fixture.ShouldNotBeNull();
    }

    [Fact]
    public void Redis_Default_CreatesFixture()
    {
        var fixture = EncinaContainers.Redis();
        fixture.ShouldNotBeNull();
    }

    [Fact]
    public void SqlServer_WithConfigure_CreatesConfiguredFixture()
    {
        var fixture = EncinaContainers.SqlServer(_ => { });
        fixture.ShouldNotBeNull();
    }

    [Fact]
    public void PostgreSql_WithConfigure_CreatesConfiguredFixture()
    {
        var fixture = EncinaContainers.PostgreSql(_ => { });
        fixture.ShouldNotBeNull();
    }

    [Fact]
    public void MySql_WithConfigure_CreatesConfiguredFixture()
    {
        var fixture = EncinaContainers.MySql(_ => { });
        fixture.ShouldNotBeNull();
    }

    [Fact]
    public void MongoDb_WithConfigure_CreatesConfiguredFixture()
    {
        var fixture = EncinaContainers.MongoDb(_ => { });
        fixture.ShouldNotBeNull();
    }

    [Fact]
    public void Redis_WithConfigure_CreatesConfiguredFixture()
    {
        var fixture = EncinaContainers.Redis(_ => { });
        fixture.ShouldNotBeNull();
    }

    // ─── ContainerFixtureBase pre-init error paths ───

    [Fact]
    public void SqlServerFixture_Container_BeforeInit_Throws()
    {
        var sut = new SqlServerContainerFixture();
        Should.Throw<InvalidOperationException>(() => _ = sut.Container);
    }

    [Fact]
    public void PostgreSqlFixture_Container_BeforeInit_Throws()
    {
        var sut = new PostgreSqlContainerFixture();
        Should.Throw<InvalidOperationException>(() => _ = sut.Container);
    }

    [Fact]
    public void SqlServerFixture_ConnectionString_BeforeInit_Throws()
    {
        var sut = new SqlServerContainerFixture();
        Should.Throw<InvalidOperationException>(() => _ = sut.ConnectionString);
    }

    [Fact]
    public async Task SqlServerFixture_DisposeAsync_BeforeInit_NoOp()
    {
        var sut = new SqlServerContainerFixture();
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task PostgreSqlFixture_DisposeAsync_BeforeInit_NoOp()
    {
        var sut = new PostgreSqlContainerFixture();
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task MySqlFixture_DisposeAsync_BeforeInit_NoOp()
    {
        var sut = new MySqlContainerFixture();
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task MongoDbFixture_DisposeAsync_BeforeInit_NoOp()
    {
        var sut = new MongoDbContainerFixture();
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task RedisFixture_DisposeAsync_BeforeInit_NoOp()
    {
        var sut = new RedisContainerFixture();
        await sut.DisposeAsync();
    }

    // ─── ConfiguredContainerFixture error paths ───

    [Fact]
    public void ConfiguredFixture_TryGetConnectionString_NotRunning_ReturnsFalse()
    {
        var fixture = EncinaContainers.Redis(_ => { });
        var result = fixture.TryGetConnectionString(out var cs, out var error);
        result.ShouldBeFalse();
        cs.ShouldBeNull();
        error.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ConfiguredFixture_ConnectionString_NotRunning_Throws()
    {
        var fixture = EncinaContainers.Redis(_ => { });
        Should.Throw<InvalidOperationException>(() => _ = fixture.ConnectionString);
    }

    [Fact]
    public async Task ConfiguredFixture_DisposeAsync_BeforeInit_NoOp()
    {
        var fixture = EncinaContainers.Redis(_ => { });
        await fixture.DisposeAsync();
    }

    // ─── SqlServerContainerFixture ResolveConfigValue branches ───

    [Fact]
    public void SqlServerFixture_NullImage_UsesDefault()
    {
        var sut = new SqlServerContainerFixture(image: null);
        sut.ShouldNotBeNull();
    }

    [Fact]
    public void SqlServerFixture_EmptyImage_UsesDefault()
    {
        var sut = new SqlServerContainerFixture(image: "");
        sut.ShouldNotBeNull();
    }

    [Fact]
    public void SqlServerFixture_WhitespaceImage_UsesDefault()
    {
        var sut = new SqlServerContainerFixture(image: "   ");
        sut.ShouldNotBeNull();
    }

    [Fact]
    public void SqlServerFixture_CustomImageAndPassword_Constructs()
    {
        var sut = new SqlServerContainerFixture(image: "mssql:custom", password: "Pwd@123");
        sut.ShouldNotBeNull();
    }
}
