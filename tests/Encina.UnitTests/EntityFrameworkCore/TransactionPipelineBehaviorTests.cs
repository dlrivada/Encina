using Encina.Testing;
using Encina.EntityFrameworkCore;
using System.Data;
using Encina.EntityFrameworkCore.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

using EncinaRequest = global::Encina.IRequest<string>;

namespace Encina.UnitTests.EntityFrameworkCore;

/// <summary>
/// Unit tests for <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
/// <remarks>
/// Note: Full integration tests for transaction behavior require a real database context
/// due to EF Core's internal dependency injection. These tests focus on constructor
/// validation. Service registration is tested in ServiceCollectionExtensionsTests.
/// </remarks>
public sealed class TransactionPipelineBehaviorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<TransactionPipelineBehavior<TestCommand, string>>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TransactionPipelineBehavior<TestCommand, string>(null!, logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TransactionPipelineBehavior<TestCommand, string>(dbContext, null!));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var logger = NullLogger<TransactionPipelineBehavior<TestCommand, string>>.Instance;

        // Act
        var behavior = new TransactionPipelineBehavior<TestCommand, string>(dbContext, logger);

        // Assert
        behavior.ShouldNotBeNull();
    }

    #endregion

    #region Helper Methods

    private static TestDbContext CreateInMemoryDbContext()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase($"TransactionTest_{Guid.NewGuid()}"));
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<TestDbContext>();
    }

    #endregion

    #region Test Commands

    private sealed record TestCommand : EncinaRequest;

    #endregion
}
