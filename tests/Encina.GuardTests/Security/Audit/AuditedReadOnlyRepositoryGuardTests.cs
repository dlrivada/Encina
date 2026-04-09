using Encina.DomainModeling;
using Encina.Security.Audit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Encina.GuardTests.Security.Audit;

/// <summary>
/// Guard clause tests for <see cref="AuditedReadOnlyRepository{TEntity, TId}"/>.
/// Verifies that null arguments to the constructor are properly rejected.
/// </summary>
public class AuditedReadOnlyRepositoryGuardTests
{
    private readonly IReadOnlyRepository<TestEntity, Guid> _inner = Substitute.For<IReadOnlyRepository<TestEntity, Guid>>();
    private readonly IReadAuditStore _store = Substitute.For<IReadAuditStore>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly IReadAuditContext _auditContext = Substitute.For<IReadAuditContext>();
    private readonly ReadAuditOptions _options = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<AuditedReadOnlyRepository<TestEntity, Guid>> _logger =
        NullLogger<AuditedReadOnlyRepository<TestEntity, Guid>>.Instance;

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new AuditedReadOnlyRepository<TestEntity, Guid>(
            null!, _store, _requestContext, _auditContext, _options, _timeProvider, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("inner");
    }

    [Fact]
    public void Constructor_NullReadAuditStore_ThrowsArgumentNullException()
    {
        var act = () => new AuditedReadOnlyRepository<TestEntity, Guid>(
            _inner, null!, _requestContext, _auditContext, _options, _timeProvider, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("readAuditStore");
    }

    [Fact]
    public void Constructor_NullRequestContext_ThrowsArgumentNullException()
    {
        var act = () => new AuditedReadOnlyRepository<TestEntity, Guid>(
            _inner, _store, null!, _auditContext, _options, _timeProvider, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("requestContext");
    }

    [Fact]
    public void Constructor_NullReadAuditContext_ThrowsArgumentNullException()
    {
        var act = () => new AuditedReadOnlyRepository<TestEntity, Guid>(
            _inner, _store, _requestContext, null!, _options, _timeProvider, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("readAuditContext");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new AuditedReadOnlyRepository<TestEntity, Guid>(
            _inner, _store, _requestContext, _auditContext, null!, _timeProvider, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new AuditedReadOnlyRepository<TestEntity, Guid>(
            _inner, _store, _requestContext, _auditContext, _options, null!, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new AuditedReadOnlyRepository<TestEntity, Guid>(
            _inner, _store, _requestContext, _auditContext, _options, _timeProvider, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        var act = () => new AuditedReadOnlyRepository<TestEntity, Guid>(
            _inner, _store, _requestContext, _auditContext, _options, _timeProvider, _logger);

        act.Should().NotThrow();
    }

    public sealed class TestEntity : IEntity<Guid>, IReadAuditable
    {
        public Guid Id { get; set; }
    }
}
