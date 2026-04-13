using Encina.Testing.Testcontainers;

using Shouldly;

namespace Encina.UnitTests.Testing.Testcontainers;

/// <summary>
/// Unit tests for container fixture types — exercising construction, pre-init state,
/// and error branches WITHOUT starting Docker containers.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ContainerFixtureTests
{
    // ─── SqlServerContainerFixture ───

    [Fact]
    public void SqlServerContainerFixture_DefaultConstructor_IsNotRunning()
    {
        var sut = new SqlServerContainerFixture();
        sut.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void SqlServerContainerFixture_CustomImage_Constructs()
    {
        var sut = new SqlServerContainerFixture(image: "custom:latest");
        sut.ShouldNotBeNull();
        sut.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void SqlServerContainerFixture_CustomPassword_Constructs()
    {
        var sut = new SqlServerContainerFixture(password: "Custom@123");
        sut.ShouldNotBeNull();
    }

    [Fact]
    public void SqlServerContainerFixture_Container_BeforeInit_Throws()
    {
        var sut = new SqlServerContainerFixture();
        Should.Throw<InvalidOperationException>(() => _ = sut.Container);
    }

    [Fact]
    public void SqlServerContainerFixture_ConnectionString_BeforeInit_Throws()
    {
        var sut = new SqlServerContainerFixture();
        Should.Throw<InvalidOperationException>(() => _ = sut.ConnectionString);
    }

    [Fact]
    public async Task SqlServerContainerFixture_DisposeAsync_BeforeInit_DoesNotThrow()
    {
        var sut = new SqlServerContainerFixture();
        await sut.DisposeAsync();
    }

    // ─── PostgreSqlContainerFixture ───

    [Fact]
    public void PostgreSqlContainerFixture_DefaultConstructor_IsNotRunning()
    {
        var sut = new PostgreSqlContainerFixture();
        sut.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void PostgreSqlContainerFixture_Container_BeforeInit_Throws()
    {
        var sut = new PostgreSqlContainerFixture();
        Should.Throw<InvalidOperationException>(() => _ = sut.Container);
    }

    [Fact]
    public async Task PostgreSqlContainerFixture_DisposeAsync_BeforeInit_DoesNotThrow()
    {
        var sut = new PostgreSqlContainerFixture();
        await sut.DisposeAsync();
    }

    // ─── MySqlContainerFixture ───

    [Fact]
    public void MySqlContainerFixture_DefaultConstructor_IsNotRunning()
    {
        var sut = new MySqlContainerFixture();
        sut.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void MySqlContainerFixture_Container_BeforeInit_Throws()
    {
        var sut = new MySqlContainerFixture();
        Should.Throw<InvalidOperationException>(() => _ = sut.Container);
    }

    [Fact]
    public async Task MySqlContainerFixture_DisposeAsync_BeforeInit_DoesNotThrow()
    {
        var sut = new MySqlContainerFixture();
        await sut.DisposeAsync();
    }

    // ─── MongoDbContainerFixture ───

    [Fact]
    public void MongoDbContainerFixture_DefaultConstructor_IsNotRunning()
    {
        var sut = new MongoDbContainerFixture();
        sut.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void MongoDbContainerFixture_Container_BeforeInit_Throws()
    {
        var sut = new MongoDbContainerFixture();
        Should.Throw<InvalidOperationException>(() => _ = sut.Container);
    }

    [Fact]
    public async Task MongoDbContainerFixture_DisposeAsync_BeforeInit_DoesNotThrow()
    {
        var sut = new MongoDbContainerFixture();
        await sut.DisposeAsync();
    }

    // ─── RedisContainerFixture ───

    [Fact]
    public void RedisContainerFixture_DefaultConstructor_IsNotRunning()
    {
        var sut = new RedisContainerFixture();
        sut.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void RedisContainerFixture_Container_BeforeInit_Throws()
    {
        var sut = new RedisContainerFixture();
        Should.Throw<InvalidOperationException>(() => _ = sut.Container);
    }

    [Fact]
    public async Task RedisContainerFixture_DisposeAsync_BeforeInit_DoesNotThrow()
    {
        var sut = new RedisContainerFixture();
        await sut.DisposeAsync();
    }

    // ─── ConfiguredContainerFixture<T> ───

    [Fact]
    public void ConfiguredContainerFixture_IsRunning_BeforeInit_IsFalse()
    {
        var fixture = EncinaContainers.Redis(_ => { });
        fixture.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void ConfiguredContainerFixture_ConnectionString_BeforeInit_Throws()
    {
        var fixture = EncinaContainers.Redis(_ => { });
        Should.Throw<InvalidOperationException>(() => _ = fixture.ConnectionString);
    }

    [Fact]
    public void ConfiguredContainerFixture_TryGetConnectionString_BeforeInit_ReturnsFalse()
    {
        var fixture = EncinaContainers.Redis(_ => { });
        var result = fixture.TryGetConnectionString(out var cs, out var error);
        result.ShouldBeFalse();
        cs.ShouldBeNull();
        error.ShouldNotBeNullOrEmpty();
        error!.ShouldContain("not running");
    }

    [Fact]
    public async Task ConfiguredContainerFixture_DisposeAsync_BeforeInit_DoesNotThrow()
    {
        var fixture = EncinaContainers.Redis(_ => { });
        await fixture.DisposeAsync();
    }
}
