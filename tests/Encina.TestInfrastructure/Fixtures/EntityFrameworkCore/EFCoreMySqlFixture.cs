using Microsoft.EntityFrameworkCore;

namespace Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;

/// <summary>
/// EF Core fixture for MySQL that wraps <see cref="MySqlFixture"/>.
/// Provides DbContext creation with Pomelo MySQL provider configuration.
/// </summary>
/// <remarks>
/// <para>
/// <b>IMPORTANT:</b> This fixture requires Pomelo.EntityFrameworkCore.MySql v10.0.0 or later,
/// which is not yet released. Until then, MySQL EF Core tests will throw NotSupportedException.
/// </para>
/// <para>
/// Track progress: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/pull/2019
/// </para>
/// </remarks>
public sealed class EFCoreMySqlFixture : IEFCoreFixture
{
    private readonly MySqlFixture _mySqlFixture = new();

    /// <inheritdoc />
    /// <remarks>
    /// Always returns <c>false</c> until Pomelo.EntityFrameworkCore.MySql v10.0.0 is released.
    /// </remarks>
    public bool IsAvailable => false; // _mySqlFixture.IsAvailable - Disabled until Pomelo 10.0.0

    /// <inheritdoc />
    public string ConnectionString => _mySqlFixture.ConnectionString;

    /// <inheritdoc />
    public string ProviderName => "MySQL";

    /// <inheritdoc />
    public TContext CreateDbContext<TContext>() where TContext : DbContext
    {
        // TODO: Uncomment when Pomelo.EntityFrameworkCore.MySql v10.0.0 is released
        // var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        // var serverVersion = ServerVersion.AutoDetect(ConnectionString);
        // optionsBuilder.UseMySql(ConnectionString, serverVersion);
        // return (TContext)Activator.CreateInstance(typeof(TContext), optionsBuilder.Options)!;

        throw new NotSupportedException(
            "MySQL EF Core fixture requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released. " +
            "Track progress at: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/pull/2019");
    }

    /// <inheritdoc />
    public Task EnsureSchemaCreatedAsync<TContext>() where TContext : DbContext
    {
        throw new NotSupportedException(
            "MySQL EF Core fixture requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released.");
    }

    /// <inheritdoc />
    public async Task ClearAllDataAsync()
    {
        await _mySqlFixture.ClearAllDataAsync();
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _mySqlFixture.InitializeAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _mySqlFixture.DisposeAsync();
    }
}
