using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Encina.EntityFrameworkCore.Outbox;

namespace Encina.EntityFrameworkCore.GuardTests;

/// <summary>
/// Guard tests for <see cref="OutboxPostProcessor{TRequest, TResponse}"/> to verify null parameter handling.
/// </summary>
public class OutboxPostProcessorGuardsTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when dbContext is null.
    /// </summary>
    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;
        var logger = Substitute.For<ILogger<OutboxPostProcessor<TestRequest, string>>>();

        // Act & Assert
        var act = () => new OutboxPostProcessor<TestRequest, string>(dbContext, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dbContext");
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
        ILogger<OutboxPostProcessor<TestRequest, string>> logger = null!;

        // Act & Assert
        var act = () => new OutboxPostProcessor<TestRequest, string>(dbContext, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that Process throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task Process_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var logger = Substitute.For<ILogger<OutboxPostProcessor<TestRequest, string>>>();
        var processor = new OutboxPostProcessor<TestRequest, string>(dbContext, logger);

        TestRequest request = null!;
        var context = Substitute.For<IRequestContext>();
        Either<MediatorError, string> response = "test";

        // Act & Assert
        var act = async () => await processor.Process(request, context, response, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
    }

    /// <summary>
    /// Verifies that Process throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task Process_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var logger = Substitute.For<ILogger<OutboxPostProcessor<TestRequest, string>>>();
        var processor = new OutboxPostProcessor<TestRequest, string>(dbContext, logger);

        var request = new TestRequest();
        IRequestContext context = null!;
        Either<MediatorError, string> response = "test";

        // Act & Assert
        var act = async () => await processor.Process(request, context, response, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
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

        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NotificationType).IsRequired();
                entity.Property(e => e.Content).IsRequired();
            });
        }
    }
}
