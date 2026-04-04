using Encina.Security.Audit;
using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Auditing;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.GuardTests.Security.Secrets.Auditing;

/// <summary>
/// Guard tests for <see cref="AuditedSecretRotatorDecorator"/> including constructor and method-level guards.
/// </summary>
public sealed class AuditedSecretRotatorDecoratorGuardTests
{
    private readonly ISecretRotator _inner = Substitute.For<ISecretRotator>();
    private readonly IAuditStore _auditStore = Substitute.For<IAuditStore>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly SecretsOptions _options = new();

    #region Constructor Guards

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretRotatorDecorator(
            null!, _auditStore, _requestContext, _options,
            NullLogger<AuditedSecretRotatorDecorator>.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretRotatorDecorator(
            _inner, null!, _requestContext, _options,
            NullLogger<AuditedSecretRotatorDecorator>.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("auditStore");
    }

    [Fact]
    public void Constructor_NullRequestContext_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretRotatorDecorator(
            _inner, _auditStore, null!, _options,
            NullLogger<AuditedSecretRotatorDecorator>.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("requestContext");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretRotatorDecorator(
            _inner, _auditStore, _requestContext, null!,
            NullLogger<AuditedSecretRotatorDecorator>.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretRotatorDecorator(
            _inner, _auditStore, _requestContext, _options, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region Method Guards - RotateSecretAsync delegates

    [Fact]
    public async Task RotateSecretAsync_AuditingDisabled_DelegatesToInner()
    {
        var options = new SecretsOptions { EnableAccessAuditing = false };
        var sut = new AuditedSecretRotatorDecorator(
            _inner, _auditStore, _requestContext, options,
            NullLogger<AuditedSecretRotatorDecorator>.Instance);

        _inner.RotateSecretAsync("test-secret", Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await sut.RotateSecretAsync("test-secret");

        result.IsRight.ShouldBeTrue();
        await _inner.Received(1).RotateSecretAsync("test-secret", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RotateSecretAsync_AuditingEnabled_RecordsEntry()
    {
        var options = new SecretsOptions { EnableAccessAuditing = true };
        _requestContext.CorrelationId.Returns("corr-123");
        _requestContext.UserId.Returns("user-1");
        _auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var sut = new AuditedSecretRotatorDecorator(
            _inner, _auditStore, _requestContext, options,
            NullLogger<AuditedSecretRotatorDecorator>.Instance);

        _inner.RotateSecretAsync("test-secret", Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await sut.RotateSecretAsync("test-secret");

        result.IsRight.ShouldBeTrue();
        await _auditStore.Received(1).RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }

    #endregion
}
