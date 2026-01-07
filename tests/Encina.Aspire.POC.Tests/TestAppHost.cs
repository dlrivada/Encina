using Aspire.Hosting;

namespace Encina.Aspire.POC.Tests;

/// <summary>
/// Entry point class for Aspire.Hosting.Testing.
/// </summary>
/// <remarks>
/// <para>
/// This class serves as the entry point for <c>DistributedApplicationTestingBuilder.CreateAsync&lt;TEntryPoint&gt;</c>.
/// It configures PostgreSQL resources for integration testing.
/// </para>
/// <para>
/// In a real Aspire application, this would be the Program class of the AppHost project.
/// For POC purposes, we configure resources directly here.
/// </para>
/// <para>
/// This class uses a private constructor and static entry point pattern
/// required by Aspire's testing infrastructure.
/// </para>
/// </remarks>
public sealed class TestAppHost
{
    /// <summary>
    /// The name of the PostgreSQL server resource.
    /// </summary>
    public const string PostgresServerName = "postgres";

    /// <summary>
    /// The name of the PostgreSQL database resource.
    /// </summary>
    public const string PostgresDatabaseName = "encina_test";

    /// <summary>
    /// Prevents external instantiation.
    /// </summary>
    private TestAppHost()
    {
    }

    /// <summary>
    /// Main entry point that configures the distributed application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // Add PostgreSQL resource
        var postgres = builder.AddPostgres(PostgresServerName)
            .WithImage("postgres")
            .WithImageTag("17-alpine");

        // Add a named database
        postgres.AddDatabase(PostgresDatabaseName);

        builder.Build().Run();
    }
}
