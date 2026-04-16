using Encina.DomainModeling;
using Encina.Security.Audit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Security.Audit.ReadAudit;

/// <summary>
/// Guard clause tests for read audit types.
/// Verifies that null arguments are properly rejected with <see cref="ArgumentNullException"/>.
/// </summary>
public class ReadAuditGuardTests
{
    #region InMemoryReadAuditStore Guard Tests

    [Fact]
    public async Task InMemoryReadAuditStore_LogReadAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemoryReadAuditStore();

        // Act
        var act = async () => await store.LogReadAsync(null!);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("entry");
    }

    [Fact]
    public async Task InMemoryReadAuditStore_GetAccessHistoryAsync_NullEntityType_ThrowsArgumentException()
    {
        // Arrange
        var store = new InMemoryReadAuditStore();

        // Act
        var act = async () => await store.GetAccessHistoryAsync(null!, "id");

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task InMemoryReadAuditStore_GetAccessHistoryAsync_EmptyEntityType_ThrowsArgumentException()
    {
        // Arrange
        var store = new InMemoryReadAuditStore();

        // Act
        var act = async () => await store.GetAccessHistoryAsync("", "id");

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task InMemoryReadAuditStore_GetAccessHistoryAsync_NullEntityId_ThrowsArgumentException()
    {
        // Arrange
        var store = new InMemoryReadAuditStore();

        // Act
        var act = async () => await store.GetAccessHistoryAsync("Patient", null!);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task InMemoryReadAuditStore_GetUserAccessHistoryAsync_NullUserId_ThrowsArgumentException()
    {
        // Arrange
        var store = new InMemoryReadAuditStore();

        // Act
        var act = async () => await store.GetUserAccessHistoryAsync(
            null!, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task InMemoryReadAuditStore_GetUserAccessHistoryAsync_EmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var store = new InMemoryReadAuditStore();

        // Act
        var act = async () => await store.GetUserAccessHistoryAsync(
            "", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task InMemoryReadAuditStore_QueryAsync_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemoryReadAuditStore();

        // Act
        var act = async () => await store.QueryAsync(null!);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("query");
    }

    #endregion

    #region ReadAuditContext Guard Tests

    [Fact]
    public void ReadAuditContext_WithPurpose_NullPurpose_ThrowsArgumentException()
    {
        // Arrange
        var context = new ReadAuditContext();

        // Act
        var act = () => context.WithPurpose(null!);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void ReadAuditContext_WithPurpose_EmptyPurpose_ThrowsArgumentException()
    {
        // Arrange
        var context = new ReadAuditContext();

        // Act
        var act = () => context.WithPurpose(string.Empty);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void ReadAuditContext_WithPurpose_WhitespacePurpose_ThrowsArgumentException()
    {
        // Arrange
        var context = new ReadAuditContext();

        // Act
        var act = () => context.WithPurpose("   ");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region ReadAuditOptions Guard Tests

    [Fact]
    public void ReadAuditOptions_IsAuditable_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act
        var act = (Action)(() => options.IsAuditable(null!));

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("entityType");
    }

    [Fact]
    public void ReadAuditOptions_GetSamplingRate_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act
        var act = (Action)(() => options.GetSamplingRate(null!));

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("entityType");
    }

    #endregion

    #region ReadAuditRetentionService Guard Tests

    [Fact]
    public void ReadAuditRetentionService_Constructor_NullStore_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new ReadAuditOptions());
        var logger = NullLogger<ReadAuditRetentionService>.Instance;

        // Act
        var act = () => new ReadAuditRetentionService(null!, options, logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("readAuditStore");
    }

    [Fact]
    public void ReadAuditRetentionService_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IReadAuditStore>();
        var logger = NullLogger<ReadAuditRetentionService>.Instance;

        // Act
        var act = () => new ReadAuditRetentionService(store, null!, logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void ReadAuditRetentionService_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IReadAuditStore>();
        var options = Options.Create(new ReadAuditOptions());

        // Act
        var act = () => new ReadAuditRetentionService(store, options, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    #endregion

    #region AuditedRepository Guard Tests

    [Fact]
    public void AuditedRepository_Constructor_NullInner_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IReadAuditStore>();
        var requestContext = Substitute.For<IRequestContext>();
        var auditContext = Substitute.For<IReadAuditContext>();
        var options = new ReadAuditOptions();
        var timeProvider = TimeProvider.System;
        var logger = NullLogger<AuditedRepository<TestAuditableEntity, Guid>>.Instance;

        // Act
        var act = () => new AuditedRepository<TestAuditableEntity, Guid>(
            null!, store, requestContext, auditContext, options, timeProvider, logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("inner");
    }

    [Fact]
    public void AuditedRepository_Constructor_NullStore_ThrowsArgumentNullException()
    {
        // Arrange
        var inner = Substitute.For<IRepository<TestAuditableEntity, Guid>>();
        var requestContext = Substitute.For<IRequestContext>();
        var auditContext = Substitute.For<IReadAuditContext>();
        var options = new ReadAuditOptions();
        var timeProvider = TimeProvider.System;
        var logger = NullLogger<AuditedRepository<TestAuditableEntity, Guid>>.Instance;

        // Act
        var act = () => new AuditedRepository<TestAuditableEntity, Guid>(
            inner, null!, requestContext, auditContext, options, timeProvider, logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("readAuditStore");
    }

    [Fact]
    public void AuditedRepository_Constructor_NullRequestContext_ThrowsArgumentNullException()
    {
        // Arrange
        var inner = Substitute.For<IRepository<TestAuditableEntity, Guid>>();
        var store = Substitute.For<IReadAuditStore>();
        var auditContext = Substitute.For<IReadAuditContext>();
        var options = new ReadAuditOptions();
        var timeProvider = TimeProvider.System;
        var logger = NullLogger<AuditedRepository<TestAuditableEntity, Guid>>.Instance;

        // Act
        var act = () => new AuditedRepository<TestAuditableEntity, Guid>(
            inner, store, null!, auditContext, options, timeProvider, logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("requestContext");
    }

    [Fact]
    public void AuditedRepository_Constructor_NullAuditContext_ThrowsArgumentNullException()
    {
        // Arrange
        var inner = Substitute.For<IRepository<TestAuditableEntity, Guid>>();
        var store = Substitute.For<IReadAuditStore>();
        var requestContext = Substitute.For<IRequestContext>();
        var options = new ReadAuditOptions();
        var timeProvider = TimeProvider.System;
        var logger = NullLogger<AuditedRepository<TestAuditableEntity, Guid>>.Instance;

        // Act
        var act = () => new AuditedRepository<TestAuditableEntity, Guid>(
            inner, store, requestContext, null!, options, timeProvider, logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("readAuditContext");
    }

    [Fact]
    public void AuditedRepository_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var inner = Substitute.For<IRepository<TestAuditableEntity, Guid>>();
        var store = Substitute.For<IReadAuditStore>();
        var requestContext = Substitute.For<IRequestContext>();
        var auditContext = Substitute.For<IReadAuditContext>();
        var timeProvider = TimeProvider.System;
        var logger = NullLogger<AuditedRepository<TestAuditableEntity, Guid>>.Instance;

        // Act
        var act = () => new AuditedRepository<TestAuditableEntity, Guid>(
            inner, store, requestContext, auditContext, null!, timeProvider, logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void AuditedRepository_Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var inner = Substitute.For<IRepository<TestAuditableEntity, Guid>>();
        var store = Substitute.For<IReadAuditStore>();
        var requestContext = Substitute.For<IRequestContext>();
        var auditContext = Substitute.For<IReadAuditContext>();
        var options = new ReadAuditOptions();
        var logger = NullLogger<AuditedRepository<TestAuditableEntity, Guid>>.Instance;

        // Act
        var act = () => new AuditedRepository<TestAuditableEntity, Guid>(
            inner, store, requestContext, auditContext, options, null!, logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void AuditedRepository_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var inner = Substitute.For<IRepository<TestAuditableEntity, Guid>>();
        var store = Substitute.For<IReadAuditStore>();
        var requestContext = Substitute.For<IRequestContext>();
        var auditContext = Substitute.For<IReadAuditContext>();
        var options = new ReadAuditOptions();
        var timeProvider = TimeProvider.System;

        // Act
        var act = () => new AuditedRepository<TestAuditableEntity, Guid>(
            inner, store, requestContext, auditContext, options, timeProvider, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    #endregion

    #region Test Types

    public sealed class TestAuditableEntity : IEntity<Guid>, IReadAuditable
    {
        public Guid Id { get; set; }
    }

    #endregion
}
