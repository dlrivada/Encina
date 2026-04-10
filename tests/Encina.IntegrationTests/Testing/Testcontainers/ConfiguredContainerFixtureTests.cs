using DotNet.Testcontainers.Containers;
using Encina.Testing.Testcontainers;
using Testcontainers.PostgreSql;

namespace Encina.IntegrationTests.Testing.Testcontainers;

/// <summary>
/// Unit tests for <see cref="ConfiguredContainerFixture{TContainer}"/>.
/// </summary>
[Trait("Category", "Integration")]
public class ConfiguredContainerFixtureTests
{
    [Fact]
    public void Constructor_WithNullContainer_ShouldThrowArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ConfiguredContainerFixture<PostgreSqlContainer>(null!));
    }

    [Fact]
    public async Task Container_ShouldReturnProvidedContainer()
    {
        var container = new PostgreSqlBuilder("postgres:16")
            .WithCleanUp(true).Build();
        var fixture = new ConfiguredContainerFixture<PostgreSqlContainer>(container);

        try { fixture.Container.ShouldBe(container); }
        finally { await container.DisposeAsync(); }
    }

    [Fact]
    public async Task IsRunning_BeforeStart_ShouldBeFalse()
    {
        var container = new PostgreSqlBuilder("postgres:16")
            .WithCleanUp(true).Build();
        var fixture = new ConfiguredContainerFixture<PostgreSqlContainer>(container);

        try { fixture.IsRunning.ShouldBeFalse(); }
        finally { await container.DisposeAsync(); }
    }

    #region TryGetConnectionString

    [Fact]
    public async Task TryGetConnectionString_WhenNotRunning_ReturnsFalse()
    {
        var container = new PostgreSqlBuilder("postgres:16")
            .WithCleanUp(true).Build();
        var fixture = new ConfiguredContainerFixture<PostgreSqlContainer>(container);

        try
        {
            var success = fixture.TryGetConnectionString(out var cs, out var error);

            success.ShouldBeFalse();
            cs.ShouldBeNull();
            error!.ShouldContain("not initialized or not running");
        }
        finally { await container.DisposeAsync(); }
    }

    [Fact]
    public async Task ConnectionString_WhenNotRunning_ShouldThrow()
    {
        var container = new PostgreSqlBuilder("postgres:16")
            .WithCleanUp(true).Build();
        var fixture = new ConfiguredContainerFixture<PostgreSqlContainer>(container);

        try
        {
            Should.Throw<InvalidOperationException>(() => _ = fixture.ConnectionString);
        }
        finally { await container.DisposeAsync(); }
    }

    #endregion

    #region Fixture Properties — Defaults

    [Fact]
    public async Task PostgreSqlDefaults_ShouldBeConfigured()
    {
        var fixture = new PostgreSqlContainerFixture();
        try
        {
            fixture.IsRunning.ShouldBeFalse();
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task SqlServerDefaults_ShouldBeConfigured()
    {
        var fixture = new SqlServerContainerFixture();
        try
        {
            fixture.IsRunning.ShouldBeFalse();
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task MySqlDefaults_ShouldBeConfigured()
    {
        var fixture = new MySqlContainerFixture();
        try
        {
            fixture.IsRunning.ShouldBeFalse();
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task MongoDbDefaults_ShouldBeConfigured()
    {
        var fixture = new MongoDbContainerFixture();
        try
        {
            fixture.IsRunning.ShouldBeFalse();
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task RedisDefaults_ShouldBeConfigured()
    {
        var fixture = new RedisContainerFixture();
        try
        {
            fixture.IsRunning.ShouldBeFalse();
        }
        finally { await fixture.DisposeAsync(); }
    }

    #endregion

    #region SqlServer Constructor Variants

    [Fact]
    public async Task SqlServer_CustomImage_ShouldNotThrow()
    {
        var fixture = new SqlServerContainerFixture("mcr.microsoft.com/mssql/server:2022-latest");
        try { fixture.IsRunning.ShouldBeFalse(); }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task SqlServer_CustomImageAndPassword_ShouldNotThrow()
    {
        var fixture = new SqlServerContainerFixture("mcr.microsoft.com/mssql/server:2022-latest", "CustomP@ss123");
        try { fixture.IsRunning.ShouldBeFalse(); }
        finally { await fixture.DisposeAsync(); }
    }

    #endregion

    #region EncinaContainers Factory

    [Fact]
    public async Task EncinaContainers_PostgreSql_CreatesFixture()
    {
        var fixture = EncinaContainers.PostgreSql();
        fixture.ShouldNotBeNull();
        fixture.IsRunning.ShouldBeFalse();
        await fixture.DisposeAsync();
    }

    [Fact]
    public async Task EncinaContainers_SqlServer_CreatesFixture()
    {
        var fixture = EncinaContainers.SqlServer();
        fixture.ShouldNotBeNull();
        fixture.IsRunning.ShouldBeFalse();
        await fixture.DisposeAsync();
    }

    [Fact]
    public async Task EncinaContainers_MySql_CreatesFixture()
    {
        var fixture = EncinaContainers.MySql();
        fixture.ShouldNotBeNull();
        fixture.IsRunning.ShouldBeFalse();
        await fixture.DisposeAsync();
    }

    [Fact]
    public async Task EncinaContainers_MongoDb_CreatesFixture()
    {
        var fixture = EncinaContainers.MongoDb();
        fixture.ShouldNotBeNull();
        fixture.IsRunning.ShouldBeFalse();
        await fixture.DisposeAsync();
    }

    [Fact]
    public async Task EncinaContainers_Redis_CreatesFixture()
    {
        var fixture = EncinaContainers.Redis();
        fixture.ShouldNotBeNull();
        fixture.IsRunning.ShouldBeFalse();
        await fixture.DisposeAsync();
    }

    #endregion
}
