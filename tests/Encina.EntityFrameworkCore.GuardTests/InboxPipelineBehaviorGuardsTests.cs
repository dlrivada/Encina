using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Encina.EntityFrameworkCore.Inbox;
using Encina.Messaging.Inbox;

namespace Encina.EntityFrameworkCore.GuardTests;

/// <summary>
/// Guard tests for <see cref="InboxPipelineBehavior{TRequest, TResponse}"/> to verify null parameter handling.
/// </summary>
public class InboxPipelineBehaviorGuardsTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when dbContext is null.
    /// </summary>
    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();

        // Act & Assert
        var act = () => new InboxPipelineBehavior<TestRequest, string>(dbContext, options, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dbContext");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(dbContextOptions);
        InboxOptions options = null!;
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();

        // Act & Assert
        var act = () => new InboxPipelineBehavior<TestRequest, string>(dbContext, options, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(dbContextOptions);
        var options = new InboxOptions();
        ILogger<InboxPipelineBehavior<TestRequest, string>> logger = null!;

        // Act & Assert
        var act = () => new InboxPipelineBehavior<TestRequest, string>(dbContext, options, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(dbContextOptions);
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();
        var behavior = new InboxPipelineBehavior<TestRequest, string>(dbContext, options, logger);

        TestRequest request = null!;
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> nextStep = () => ValueTask.FromResult<Either<MediatorError, string>>("test");

        // Act & Assert
        var act = async () => await behavior.Handle(request, context, nextStep, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(dbContextOptions);
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();
        var behavior = new InboxPipelineBehavior<TestRequest, string>(dbContext, options, logger);

        var request = new TestRequest();
        IRequestContext context = null!;
        RequestHandlerCallback<string> nextStep = () => ValueTask.FromResult<Either<MediatorError, string>>("test");

        // Act & Assert
        var act = async () => await behavior.Handle(request, context, nextStep, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when nextStep is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        // Arrange
        var dbContextOptions = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(dbContextOptions);
        var options = new InboxOptions();
        var logger = Substitute.For<ILogger<InboxPipelineBehavior<TestRequest, string>>>();
        var behavior = new InboxPipelineBehavior<TestRequest, string>(dbContext, options, logger);

        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> nextStep = null!;

        // Act & Assert
        var act = async () => await behavior.Handle(request, context, nextStep, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("nextStep");
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

        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InboxMessage>(entity =>
            {
                entity.HasKey(e => e.MessageId);
                entity.Property(e => e.RequestType).IsRequired();
                entity.Property(e => e.ReceivedAtUtc).IsRequired();
            });
        }
    }
}
