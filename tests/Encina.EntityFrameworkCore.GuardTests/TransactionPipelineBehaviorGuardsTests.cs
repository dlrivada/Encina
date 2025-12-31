using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Encina.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.GuardTests;

/// <summary>
/// Guard tests for <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/> to verify null parameter handling.
/// </summary>
public class TransactionPipelineBehaviorGuardsTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when dbContext is null.
    /// </summary>
    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;
        var logger = Substitute.For<ILogger<TransactionPipelineBehavior<TestRequest, string>>>();

        // Act & Assert
        var act = () => new TransactionPipelineBehavior<TestRequest, string>(dbContext, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("dbContext");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        ILogger<TransactionPipelineBehavior<TestRequest, string>> logger = null!;

        // Act & Assert
        var act = () => new TransactionPipelineBehavior<TestRequest, string>(dbContext, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var logger = Substitute.For<ILogger<TransactionPipelineBehavior<TestRequest, string>>>();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(dbContext, logger);

        TestRequest request = null!;
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> nextStep = () => ValueTask.FromResult<Either<EncinaError, string>>("test");

        // Act & Assert
        var act = async () => await behavior.Handle(request, context, nextStep, CancellationToken.None);
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await act());
        ex.ParamName.ShouldBe("request");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var logger = Substitute.For<ILogger<TransactionPipelineBehavior<TestRequest, string>>>();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(dbContext, logger);

        var request = new TestRequest();
        IRequestContext context = null!;
        RequestHandlerCallback<string> nextStep = () => ValueTask.FromResult<Either<EncinaError, string>>("test");

        // Act & Assert
        var act = async () => await behavior.Handle(request, context, nextStep, CancellationToken.None);
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await act());
        ex.ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when nextStep is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var logger = Substitute.For<ILogger<TransactionPipelineBehavior<TestRequest, string>>>();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(dbContext, logger);

        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> nextStep = null!;

        // Act & Assert
        var act = async () => await behavior.Handle(request, context, nextStep, CancellationToken.None);
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await act());
        ex.ParamName.ShouldBe("nextStep");
    }

    /// <summary>
    /// Test request for testing.
    /// </summary>
    public sealed record TestRequest : IRequest<string>;

    /// <summary>
    /// Test DbContext for in-memory database testing.
    /// </summary>
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options)
        {
        }
    }
}
