using System.Diagnostics.CodeAnalysis;
using Encina.DomainModeling;
using Encina.EntityFrameworkCore.DomainEvents;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Encina.UnitTests.EntityFrameworkCore.DomainEvents;

/// <summary>
/// Tests for DomainEventDispatcherInterceptor.
/// </summary>
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Mock setup pattern for NSubstitute")]
public class DomainEventDispatcherInterceptorTests
{
    #region Test Types

    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestAggregate() : base(Guid.NewGuid()) { }
        public TestAggregate(Guid id) : base(id) { }

        public void RaiseEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed record TestNotificationEvent(Guid EntityId) : IDomainEvent, INotification
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }

    private sealed record NonNotificationEvent(string Data) : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<TestAggregate> TestAggregates => Set<TestAggregate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestAggregate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Ignore(e => e.DomainEvents);
                entity.Ignore(e => e.RowVersion);
            });
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new DomainEventDispatcherOptions();
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DomainEventDispatcherInterceptor(null!, options, logger));
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DomainEventDispatcherInterceptor(serviceProvider, null!, logger));
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new DomainEventDispatcherOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DomainEventDispatcherInterceptor(serviceProvider, options, null!));
    }

    #endregion

    #region SavedChangesAsync Tests with InMemory Database

    [Fact]
    public async Task SavedChangesAsync_WithNotificationEvents_ShouldPublishEvents()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var services = new ServiceCollection();
        services.AddSingleton(encina);
        var serviceProvider = services.BuildServiceProvider();

        var options = new DomainEventDispatcherOptions { Enabled = true };
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();
        var interceptor = new DomainEventDispatcherInterceptor(serviceProvider, options, logger);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new TestDbContext(dbOptions);

        var aggregate = new TestAggregate();
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));

        context.TestAggregates.Add(aggregate);

        // Act
        await context.SaveChangesAsync();

        // Assert
        await encina.Received(2).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavedChangesAsync_DisabledOptions_ShouldNotPublishEvents()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();

        var services = new ServiceCollection();
        services.AddSingleton(encina);
        var serviceProvider = services.BuildServiceProvider();

        var options = new DomainEventDispatcherOptions { Enabled = false };
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();
        var interceptor = new DomainEventDispatcherInterceptor(serviceProvider, options, logger);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new TestDbContext(dbOptions);

        var aggregate = new TestAggregate();
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));

        context.TestAggregates.Add(aggregate);

        // Act
        await context.SaveChangesAsync();

        // Assert
        await encina.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavedChangesAsync_NonNotificationEvents_ShouldBeSkipped()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var services = new ServiceCollection();
        services.AddSingleton(encina);
        var serviceProvider = services.BuildServiceProvider();

        var options = new DomainEventDispatcherOptions { Enabled = true };
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();
        var interceptor = new DomainEventDispatcherInterceptor(serviceProvider, options, logger);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new TestDbContext(dbOptions);

        var aggregate = new TestAggregate();
        aggregate.RaiseEvent(new NonNotificationEvent("data")); // Not INotification

        context.TestAggregates.Add(aggregate);

        // Act
        await context.SaveChangesAsync();

        // Assert - Non-notification events should be skipped
        await encina.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavedChangesAsync_MixedEvents_ShouldOnlyPublishNotifications()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var services = new ServiceCollection();
        services.AddSingleton(encina);
        var serviceProvider = services.BuildServiceProvider();

        var options = new DomainEventDispatcherOptions { Enabled = true };
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();
        var interceptor = new DomainEventDispatcherInterceptor(serviceProvider, options, logger);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new TestDbContext(dbOptions);

        var aggregate = new TestAggregate();
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id)); // INotification
        aggregate.RaiseEvent(new NonNotificationEvent("skip"));         // Not INotification
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id)); // INotification

        context.TestAggregates.Add(aggregate);

        // Act
        await context.SaveChangesAsync();

        // Assert
        await encina.Received(2).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavedChangesAsync_MultipleEntities_ShouldPublishAllEvents()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var services = new ServiceCollection();
        services.AddSingleton(encina);
        var serviceProvider = services.BuildServiceProvider();

        var options = new DomainEventDispatcherOptions { Enabled = true };
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();
        var interceptor = new DomainEventDispatcherInterceptor(serviceProvider, options, logger);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new TestDbContext(dbOptions);

        var aggregate1 = new TestAggregate();
        var aggregate2 = new TestAggregate();
        var aggregate3 = new TestAggregate();

        aggregate1.RaiseEvent(new TestNotificationEvent(aggregate1.Id));
        aggregate2.RaiseEvent(new TestNotificationEvent(aggregate2.Id));
        aggregate2.RaiseEvent(new TestNotificationEvent(aggregate2.Id));
        aggregate3.RaiseEvent(new TestNotificationEvent(aggregate3.Id));

        context.TestAggregates.AddRange(aggregate1, aggregate2, aggregate3);

        // Act
        await context.SaveChangesAsync();

        // Assert
        await encina.Received(4).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavedChangesAsync_NoEvents_ShouldNotPublishAnything()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();

        var services = new ServiceCollection();
        services.AddSingleton(encina);
        var serviceProvider = services.BuildServiceProvider();

        var options = new DomainEventDispatcherOptions { Enabled = true };
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();
        var interceptor = new DomainEventDispatcherInterceptor(serviceProvider, options, logger);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new TestDbContext(dbOptions);

        var aggregate = new TestAggregate(); // No events raised

        context.TestAggregates.Add(aggregate);

        // Act
        await context.SaveChangesAsync();

        // Assert
        await encina.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavedChangesAsync_ClearEventsAfterDispatch_ShouldClearEvents()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var services = new ServiceCollection();
        services.AddSingleton(encina);
        var serviceProvider = services.BuildServiceProvider();

        var options = new DomainEventDispatcherOptions
        {
            Enabled = true,
            ClearEventsAfterDispatch = true
        };
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();
        var interceptor = new DomainEventDispatcherInterceptor(serviceProvider, options, logger);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new TestDbContext(dbOptions);

        var aggregate = new TestAggregate();
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));

        context.TestAggregates.Add(aggregate);

        // Act
        await context.SaveChangesAsync();

        // Assert
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task SavedChangesAsync_StopOnFirstError_ShouldThrowException()
    {
        // Arrange
        var error = EncinaErrors.Create("TEST_ERROR", "Test error message");
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(error));

        var services = new ServiceCollection();
        services.AddSingleton(encina);
        var serviceProvider = services.BuildServiceProvider();

        var options = new DomainEventDispatcherOptions
        {
            Enabled = true,
            StopOnFirstError = true
        };
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();
        var interceptor = new DomainEventDispatcherInterceptor(serviceProvider, options, logger);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new TestDbContext(dbOptions);

        var aggregate = new TestAggregate();
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));

        context.TestAggregates.Add(aggregate);

        // Act & Assert
        await Should.ThrowAsync<DomainEventDispatchException>(async () =>
            await context.SaveChangesAsync());
    }

    [Fact]
    public async Task SavedChangesAsync_DontStopOnFirstError_ShouldContinue()
    {
        // Arrange
        var error = EncinaErrors.Create("TEST_ERROR", "Test error message");
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(
                new ValueTask<Either<EncinaError, Unit>>(error),
                new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var services = new ServiceCollection();
        services.AddSingleton(encina);
        var serviceProvider = services.BuildServiceProvider();

        var options = new DomainEventDispatcherOptions
        {
            Enabled = true,
            StopOnFirstError = false
        };
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();
        var interceptor = new DomainEventDispatcherInterceptor(serviceProvider, options, logger);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new TestDbContext(dbOptions);

        var aggregate = new TestAggregate();
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id)); // Will fail
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id)); // Should still be called

        context.TestAggregates.Add(aggregate);

        // Act - Should not throw
        await Should.NotThrowAsync(async () => await context.SaveChangesAsync());

        // Assert - Both events should have been attempted
        await encina.Received(2).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Pre-Save Event Collection Tests (CollectEventsBeforeSave Option)

    [Fact]
    public async Task SavedChangesAsync_CollectEventsBeforeSave_ShouldCollectEventsInSavingChanges()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var services = new ServiceCollection();
        services.AddSingleton(encina);
        var serviceProvider = services.BuildServiceProvider();

        var options = new DomainEventDispatcherOptions
        {
            Enabled = true,
            CollectEventsBeforeSave = true // Default behavior
        };
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();
        var interceptor = new DomainEventDispatcherInterceptor(serviceProvider, options, logger);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new TestDbContext(dbOptions);

        var aggregate = new TestAggregate();
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));

        context.TestAggregates.Add(aggregate);

        // Act
        await context.SaveChangesAsync();

        // Assert - Events should be published even with pre-save collection
        await encina.Received(1).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavedChangesAsync_CollectEventsBeforeSaveFalse_ShouldStillPublishEvents()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var services = new ServiceCollection();
        services.AddSingleton(encina);
        var serviceProvider = services.BuildServiceProvider();

        var options = new DomainEventDispatcherOptions
        {
            Enabled = true,
            CollectEventsBeforeSave = false // Legacy behavior: collect in SavedChangesAsync
        };
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();
        var interceptor = new DomainEventDispatcherInterceptor(serviceProvider, options, logger);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new TestDbContext(dbOptions);

        var aggregate = new TestAggregate();
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));

        context.TestAggregates.Add(aggregate);

        // Act
        await context.SaveChangesAsync();

        // Assert - Events should still be published with legacy behavior
        await encina.Received(2).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavedChangesAsync_CollectEventsBeforeSave_MultipleAggregates_ShouldPublishAllEvents()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var services = new ServiceCollection();
        services.AddSingleton(encina);
        var serviceProvider = services.BuildServiceProvider();

        var options = new DomainEventDispatcherOptions
        {
            Enabled = true,
            CollectEventsBeforeSave = true
        };
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();
        var interceptor = new DomainEventDispatcherInterceptor(serviceProvider, options, logger);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new TestDbContext(dbOptions);

        var aggregate1 = new TestAggregate();
        var aggregate2 = new TestAggregate();

        aggregate1.RaiseEvent(new TestNotificationEvent(aggregate1.Id));
        aggregate2.RaiseEvent(new TestNotificationEvent(aggregate2.Id));
        aggregate2.RaiseEvent(new TestNotificationEvent(aggregate2.Id));

        context.TestAggregates.AddRange(aggregate1, aggregate2);

        // Act
        await context.SaveChangesAsync();

        // Assert - All events from all aggregates should be published
        await encina.Received(3).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavedChangesAsync_CollectEventsBeforeSave_ClearsEventsAfterDispatch()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var services = new ServiceCollection();
        services.AddSingleton(encina);
        var serviceProvider = services.BuildServiceProvider();

        var options = new DomainEventDispatcherOptions
        {
            Enabled = true,
            CollectEventsBeforeSave = true,
            ClearEventsAfterDispatch = true
        };
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();
        var interceptor = new DomainEventDispatcherInterceptor(serviceProvider, options, logger);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new TestDbContext(dbOptions);

        var aggregate = new TestAggregate();
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));

        context.TestAggregates.Add(aggregate);

        // Act
        await context.SaveChangesAsync();

        // Assert - Events should be cleared from the aggregate
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void SavingChanges_Sync_CollectEventsBeforeSave_ShouldNotThrow()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var services = new ServiceCollection();
        services.AddSingleton(encina);
        var serviceProvider = services.BuildServiceProvider();

        var options = new DomainEventDispatcherOptions
        {
            Enabled = true,
            CollectEventsBeforeSave = true
        };
        var logger = Substitute.For<ILogger<DomainEventDispatcherInterceptor>>();
        var interceptor = new DomainEventDispatcherInterceptor(serviceProvider, options, logger);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        using var context = new TestDbContext(dbOptions);

        var aggregate = new TestAggregate();
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));

        context.TestAggregates.Add(aggregate);

        // Act & Assert - Synchronous save should work without throwing
        Should.NotThrow(() => context.SaveChanges());
    }

    #endregion
}

/// <summary>
/// Tests for DomainEventDispatcherOptions.
/// </summary>
public class DomainEventDispatcherOptionsTests
{
    [Fact]
    public void DefaultOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new DomainEventDispatcherOptions();

        // Assert - defaults match DomainEventDispatcherOptions class definition
        options.Enabled.ShouldBeTrue();
        options.StopOnFirstError.ShouldBeFalse(); // Default is false (continue on error)
        options.RequireINotification.ShouldBeTrue(); // Default is true
        options.ClearEventsAfterDispatch.ShouldBeTrue();
        options.CollectEventsBeforeSave.ShouldBeTrue(); // Default is true (new option for immutable records)
    }

    [Fact]
    public void Options_ShouldBeConfigurable()
    {
        // Arrange & Act - Set values opposite to defaults
        var options = new DomainEventDispatcherOptions
        {
            Enabled = false,          // default is true
            StopOnFirstError = true,  // default is false
            RequireINotification = false, // default is true
            ClearEventsAfterDispatch = false, // default is true
            CollectEventsBeforeSave = false // default is true
        };

        // Assert
        options.Enabled.ShouldBeFalse();
        options.StopOnFirstError.ShouldBeTrue();
        options.RequireINotification.ShouldBeFalse();
        options.ClearEventsAfterDispatch.ShouldBeFalse();
        options.CollectEventsBeforeSave.ShouldBeFalse();
    }
}

/// <summary>
/// Tests for DomainEventDispatchException.
/// </summary>
public class DomainEventDispatchExceptionTests
{
    private sealed record TestEvent(Guid Id) : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }

    [Fact]
    public void Constructor_WithEncinaError_ShouldSetProperties()
    {
        // Arrange
        var domainEvent = new TestEvent(Guid.NewGuid());
        var error = EncinaErrors.Create("CODE", "message");

        // Act
        var exception = new DomainEventDispatchException("Test message", domainEvent, error);

        // Assert
        exception.Message.ShouldBe("Test message");
        exception.DomainEvent.ShouldBe(domainEvent);
        exception.EncinaError.ShouldBe(error);
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldSetProperties()
    {
        // Arrange
        var domainEvent = new TestEvent(Guid.NewGuid());
        var innerException = new InvalidOperationException("Inner");

        // Act
        var exception = new DomainEventDispatchException("Test message", domainEvent, innerException);

        // Assert
        exception.Message.ShouldBe("Test message");
        exception.DomainEvent.ShouldBe(domainEvent);
        exception.InnerException.ShouldBe(innerException);
        exception.EncinaError.ShouldBeNull();
    }
}
