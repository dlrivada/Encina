#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Audit;
using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Auditing;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets;

public sealed class AuditedSecretWriterDecoratorTests
{
    private readonly ISecretWriter _innerWriter;
    private readonly IAuditStore _auditStore;
    private readonly IRequestContext _requestContext;
    private readonly ILogger<AuditedSecretWriterDecorator> _logger;

    public AuditedSecretWriterDecoratorTests()
    {
        _innerWriter = Substitute.For<ISecretWriter>();
        _auditStore = Substitute.For<IAuditStore>();
        _requestContext = Substitute.For<IRequestContext>();
        _logger = Substitute.For<ILogger<AuditedSecretWriterDecorator>>();

        _requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        _requestContext.UserId.Returns("test-user");
        _requestContext.TenantId.Returns("test-tenant");

        _auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));
    }

    #region Constructor Validation

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var options = CreateOptions(true);

        var act = () => new AuditedSecretWriterDecorator(null!, _auditStore, _requestContext, options, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("inner");
    }

    [Fact]
    public void Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        var options = CreateOptions(true);

        var act = () => new AuditedSecretWriterDecorator(_innerWriter, null!, _requestContext, options, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("auditStore");
    }

    [Fact]
    public void Constructor_NullRequestContext_ThrowsArgumentNullException()
    {
        var options = CreateOptions(true);

        var act = () => new AuditedSecretWriterDecorator(_innerWriter, _auditStore, null!, options, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("requestContext");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretWriterDecorator(_innerWriter, _auditStore, _requestContext, null!, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var options = CreateOptions(true);

        var act = () => new AuditedSecretWriterDecorator(_innerWriter, _auditStore, _requestContext, options, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region SetSecretAsync - Auditing Disabled

    [Fact]
    public async Task SetSecretAsync_AuditingDisabled_PassesThroughToInner()
    {
        var decorator = CreateDecorator(enableAuditing: false);
        _innerWriter.SetSecretAsync("key", "value", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        var result = await decorator.SetSecretAsync("key", "value");

        result.IsRight.Should().BeTrue();
        await _auditStore.DidNotReceive().RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region SetSecretAsync - Auditing Enabled

    [Fact]
    public async Task SetSecretAsync_AuditingEnabled_RecordsAuditEntry()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerWriter.SetSecretAsync("db-password", "new-secret", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        await decorator.SetSecretAsync("db-password", "new-secret");

        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.Action == "SecretWrite" &&
                e.EntityType == "Secret" &&
                e.EntityId == "db-password" &&
                e.Outcome == AuditOutcome.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetSecretAsync_AuditingEnabled_ReturnsInnerResult()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerWriter.SetSecretAsync("key", "value", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        var result = await decorator.SetSecretAsync("key", "value");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task SetSecretAsync_AuditingEnabled_FailedWrite_RecordsFailureOutcome()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerWriter.SetSecretAsync("key", "value", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(
                SecretsErrors.AccessDenied("key", "insufficient permissions")));

        var result = await decorator.SetSecretAsync("key", "value");

        result.IsLeft.Should().BeTrue();
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.Outcome == AuditOutcome.Failure &&
                e.EntityId == "key"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetSecretAsync_AuditingEnabled_SetsUserAndTenant()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerWriter.SetSecretAsync("key", "value", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        await decorator.SetSecretAsync("key", "value");

        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.UserId == "test-user" &&
                e.TenantId == "test-tenant"),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Audit Failure Resilience

    [Fact]
    public async Task SetSecretAsync_AuditStoreFails_StillReturnsWriteResult()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerWriter.SetSecretAsync("key", "value", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        _auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(
                SecretsErrors.AuditFailed("key")));

        var result = await decorator.SetSecretAsync("key", "value");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task SetSecretAsync_AuditStoreThrows_StillReturnsWriteResult()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerWriter.SetSecretAsync("key", "value", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        _auditStore.When(x => x.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("audit store crashed"));

        var result = await decorator.SetSecretAsync("key", "value");

        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Helpers

    private AuditedSecretWriterDecorator CreateDecorator(bool enableAuditing) =>
        new(_innerWriter, _auditStore, _requestContext,
            CreateOptions(enableAuditing), _logger);

    private static SecretsOptions CreateOptions(bool enableAuditing) =>
        new() { EnableAccessAuditing = enableAuditing };

    #endregion
}
