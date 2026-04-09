using Encina.DomainModeling;
using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Encina.ContractTests.Security.Audit;

/// <summary>
/// Contract tests for <see cref="AuditedRepository{TEntity, TId}"/> and
/// <see cref="AuditedReadOnlyRepository{TEntity, TId}"/> decorators.
/// Verifies that these decorators correctly delegate to the inner repository
/// and follow the contract of their respective interfaces.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "Audit")]
public sealed class AuditedRepositoryContractTests
{
    #region AuditedReadOnlyRepository Contract

    [Fact]
    public async Task AuditedReadOnlyRepository_GetByIdAsync_DelegatesToInner()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId };
        var inner = Substitute.For<IReadOnlyRepository<TestEntity, Guid>>();
        inner.GetByIdAsync(entityId, Arg.Any<CancellationToken>())
            .Returns(Option<TestEntity>.Some(entity));

        var sut = CreateReadOnlyRepository(inner, samplingRate: 0.0);

        // Act
        var result = await sut.GetByIdAsync(entityId);

        // Assert
        result.IsSome.ShouldBeTrue("GetByIdAsync must delegate to inner and return its result");
        await inner.Received(1).GetByIdAsync(entityId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuditedReadOnlyRepository_GetAllAsync_DelegatesToInner()
    {
        // Arrange
        var entities = new List<TestEntity> { new() { Id = Guid.NewGuid() } };
        var inner = Substitute.For<IReadOnlyRepository<TestEntity, Guid>>();
        inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(entities.AsReadOnly());

        var sut = CreateReadOnlyRepository(inner, samplingRate: 0.0);

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Count.ShouldBe(1);
        await inner.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuditedReadOnlyRepository_CountAsync_DelegatesToInner_WithoutAuditing()
    {
        // Arrange
        var inner = Substitute.For<IReadOnlyRepository<TestEntity, Guid>>();
        inner.CountAsync(Arg.Any<CancellationToken>()).Returns(42);

        var sut = CreateReadOnlyRepository(inner, samplingRate: 1.0);

        // Act
        var result = await sut.CountAsync();

        // Assert
        result.ShouldBe(42, "CountAsync must delegate directly without auditing");
        await inner.Received(1).CountAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuditedReadOnlyRepository_AnyAsync_DelegatesToInner_WithoutAuditing()
    {
        // Arrange
        var inner = Substitute.For<IReadOnlyRepository<TestEntity, Guid>>();
        inner.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TestEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = CreateReadOnlyRepository(inner, samplingRate: 1.0);

        // Act
        var result = await sut.AnyAsync(e => e.Id != Guid.Empty);

        // Assert
        result.ShouldBeTrue("AnyAsync must delegate directly without auditing");
    }

    [Fact]
    public void AuditedReadOnlyRepository_ImplementsIReadOnlyRepository()
    {
        var inner = Substitute.For<IReadOnlyRepository<TestEntity, Guid>>();
        var sut = CreateReadOnlyRepository(inner, samplingRate: 0.0);

        sut.ShouldBeAssignableTo<IReadOnlyRepository<TestEntity, Guid>>(
            "AuditedReadOnlyRepository must implement IReadOnlyRepository");
    }

    #endregion

    #region AuditedRepository Contract

    [Fact]
    public async Task AuditedRepository_GetByIdAsync_DelegatesToInner()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId };
        var inner = Substitute.For<IRepository<TestEntity, Guid>>();
        inner.GetByIdAsync(entityId, Arg.Any<CancellationToken>())
            .Returns(Option<TestEntity>.Some(entity));

        var sut = CreateRepository(inner, samplingRate: 0.0);

        // Act
        var result = await sut.GetByIdAsync(entityId);

        // Assert
        result.IsSome.ShouldBeTrue("GetByIdAsync must delegate to inner");
        await inner.Received(1).GetByIdAsync(entityId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuditedRepository_AddAsync_DelegatesToInner_WithoutAuditing()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var inner = Substitute.For<IRepository<TestEntity, Guid>>();
        var auditStore = Substitute.For<IReadAuditStore>();

        var sut = CreateRepository(inner, samplingRate: 1.0, auditStore: auditStore);

        // Act
        await sut.AddAsync(entity);

        // Assert
        await inner.Received(1).AddAsync(entity, Arg.Any<CancellationToken>());
        // Verify no read audit entries were logged for a write operation
        await auditStore.DidNotReceive().LogReadAsync(Arg.Any<ReadAuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AuditedRepository_Update_DelegatesToInner_WithoutAuditing()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var inner = Substitute.For<IRepository<TestEntity, Guid>>();

        var sut = CreateRepository(inner, samplingRate: 1.0);

        // Act
        sut.Update(entity);

        // Assert
        inner.Received(1).Update(entity);
    }

    [Fact]
    public void AuditedRepository_Remove_DelegatesToInner_WithoutAuditing()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var inner = Substitute.For<IRepository<TestEntity, Guid>>();

        var sut = CreateRepository(inner, samplingRate: 1.0);

        // Act
        sut.Remove(entity);

        // Assert
        inner.Received(1).Remove(entity);
    }

    [Fact]
    public void AuditedRepository_ImplementsIRepository()
    {
        var inner = Substitute.For<IRepository<TestEntity, Guid>>();
        var sut = CreateRepository(inner, samplingRate: 0.0);

        sut.ShouldBeAssignableTo<IRepository<TestEntity, Guid>>(
            "AuditedRepository must implement IRepository");
    }

    [Fact]
    public async Task AuditedRepository_CountAsync_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IRepository<TestEntity, Guid>>();
        inner.CountAsync(Arg.Any<CancellationToken>()).Returns(7);

        var sut = CreateRepository(inner, samplingRate: 0.0);

        // Act
        var result = await sut.CountAsync();

        // Assert
        result.ShouldBe(7);
    }

    #endregion

    #region Helpers

    private static AuditedReadOnlyRepository<TestEntity, Guid> CreateReadOnlyRepository(
        IReadOnlyRepository<TestEntity, Guid> inner,
        double samplingRate,
        IReadAuditStore? auditStore = null)
    {
        var store = auditStore ?? Substitute.For<IReadAuditStore>();
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.UserId.Returns("test-user");
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        var auditContext = Substitute.For<IReadAuditContext>();
        var options = new ReadAuditOptions();
        options.AuditReadsFor<TestEntity>(samplingRate);
        var logger = NullLogger<AuditedReadOnlyRepository<TestEntity, Guid>>.Instance;

        return new AuditedReadOnlyRepository<TestEntity, Guid>(
            inner, store, requestContext, auditContext, options, TimeProvider.System, logger);
    }

    private static AuditedRepository<TestEntity, Guid> CreateRepository(
        IRepository<TestEntity, Guid> inner,
        double samplingRate,
        IReadAuditStore? auditStore = null)
    {
        var store = auditStore ?? Substitute.For<IReadAuditStore>();
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.UserId.Returns("test-user");
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        var auditContext = Substitute.For<IReadAuditContext>();
        var options = new ReadAuditOptions();
        options.AuditReadsFor<TestEntity>(samplingRate);
        var logger = NullLogger<AuditedRepository<TestEntity, Guid>>.Instance;

        return new AuditedRepository<TestEntity, Guid>(
            inner, store, requestContext, auditContext, options, TimeProvider.System, logger);
    }

    public sealed class TestEntity : IEntity<Guid>, IReadAuditable
    {
        public Guid Id { get; set; }
    }

    #endregion
}
