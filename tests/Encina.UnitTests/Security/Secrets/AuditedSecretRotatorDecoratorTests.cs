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

public sealed class AuditedSecretRotatorDecoratorTests
{
    private readonly ISecretRotator _innerRotator;
    private readonly IAuditStore _auditStore;
    private readonly IRequestContext _requestContext;
    private readonly ILogger<AuditedSecretRotatorDecorator> _logger;

    public AuditedSecretRotatorDecoratorTests()
    {
        _innerRotator = Substitute.For<ISecretRotator>();
        _auditStore = Substitute.For<IAuditStore>();
        _requestContext = Substitute.For<IRequestContext>();
        _logger = Substitute.For<ILogger<AuditedSecretRotatorDecorator>>();

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

        var act = () => new AuditedSecretRotatorDecorator(null!, _auditStore, _requestContext, options, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("inner");
    }

    [Fact]
    public void Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        var options = CreateOptions(true);

        var act = () => new AuditedSecretRotatorDecorator(_innerRotator, null!, _requestContext, options, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("auditStore");
    }

    [Fact]
    public void Constructor_NullRequestContext_ThrowsArgumentNullException()
    {
        var options = CreateOptions(true);

        var act = () => new AuditedSecretRotatorDecorator(_innerRotator, _auditStore, null!, options, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("requestContext");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretRotatorDecorator(_innerRotator, _auditStore, _requestContext, null!, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var options = CreateOptions(true);

        var act = () => new AuditedSecretRotatorDecorator(_innerRotator, _auditStore, _requestContext, options, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region RotateSecretAsync - Auditing Disabled

    [Fact]
    public async Task RotateSecretAsync_AuditingDisabled_PassesThroughToInner()
    {
        var decorator = CreateDecorator(enableAuditing: false);
        _innerRotator.RotateSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        var result = await decorator.RotateSecretAsync("key");

        result.IsRight.Should().BeTrue();
        await _auditStore.DidNotReceive().RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region RotateSecretAsync - Auditing Enabled

    [Fact]
    public async Task RotateSecretAsync_AuditingEnabled_RecordsAuditEntry()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerRotator.RotateSecretAsync("db-password", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        await decorator.RotateSecretAsync("db-password");

        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.Action == "SecretRotation" &&
                e.EntityType == "Secret" &&
                e.EntityId == "db-password" &&
                e.Outcome == AuditOutcome.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RotateSecretAsync_AuditingEnabled_ReturnsInnerResult()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerRotator.RotateSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        var result = await decorator.RotateSecretAsync("key");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RotateSecretAsync_AuditingEnabled_FailedRotation_RecordsFailureOutcome()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerRotator.RotateSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(
                SecretsErrors.RotationFailed("key", "provider error")));

        var result = await decorator.RotateSecretAsync("key");

        result.IsLeft.Should().BeTrue();
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.Outcome == AuditOutcome.Failure &&
                e.EntityId == "key"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RotateSecretAsync_AuditingEnabled_SetsUserAndTenant()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerRotator.RotateSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        await decorator.RotateSecretAsync("key");

        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.UserId == "test-user" &&
                e.TenantId == "test-tenant"),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Audit Failure Resilience

    [Fact]
    public async Task RotateSecretAsync_AuditStoreFails_StillReturnsRotationResult()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerRotator.RotateSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        _auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(
                SecretsErrors.AuditFailed("key")));

        var result = await decorator.RotateSecretAsync("key");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RotateSecretAsync_AuditStoreThrows_StillReturnsRotationResult()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerRotator.RotateSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        _auditStore.When(x => x.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("audit store crashed"));

        var result = await decorator.RotateSecretAsync("key");

        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Helpers

    private AuditedSecretRotatorDecorator CreateDecorator(bool enableAuditing) =>
        new(_innerRotator, _auditStore, _requestContext,
            CreateOptions(enableAuditing), _logger);

    private static SecretsOptions CreateOptions(bool enableAuditing) =>
        new() { EnableAccessAuditing = enableAuditing };

    #endregion
}
