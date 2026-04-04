using Encina.Security.Audit;
using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Auditing;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.GuardTests.Security.Secrets.Auditing;

/// <summary>
/// Guard tests for <see cref="AuditedSecretWriterDecorator"/> including constructor and method-level guards.
/// </summary>
public sealed class AuditedSecretWriterDecoratorGuardTests
{
    private readonly ISecretWriter _inner = Substitute.For<ISecretWriter>();
    private readonly IAuditStore _auditStore = Substitute.For<IAuditStore>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly SecretsOptions _options = new();

    #region Constructor Guards

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretWriterDecorator(
            null!, _auditStore, _requestContext, _options,
            NullLogger<AuditedSecretWriterDecorator>.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretWriterDecorator(
            _inner, null!, _requestContext, _options,
            NullLogger<AuditedSecretWriterDecorator>.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("auditStore");
    }

    [Fact]
    public void Constructor_NullRequestContext_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretWriterDecorator(
            _inner, _auditStore, null!, _options,
            NullLogger<AuditedSecretWriterDecorator>.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("requestContext");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretWriterDecorator(
            _inner, _auditStore, _requestContext, null!,
            NullLogger<AuditedSecretWriterDecorator>.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretWriterDecorator(
            _inner, _auditStore, _requestContext, _options, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region Method Guards - SetSecretAsync delegates

    [Fact]
    public async Task SetSecretAsync_AuditingDisabled_DelegatesToInner()
    {
        var options = new SecretsOptions { EnableAccessAuditing = false };
        var sut = new AuditedSecretWriterDecorator(
            _inner, _auditStore, _requestContext, options,
            NullLogger<AuditedSecretWriterDecorator>.Instance);

        _inner.SetSecretAsync("test-secret", "value", Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await sut.SetSecretAsync("test-secret", "value");

        result.IsRight.ShouldBeTrue();
        await _inner.Received(1).SetSecretAsync("test-secret", "value", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetSecretAsync_AuditingEnabled_RecordsEntry()
    {
        var options = new SecretsOptions { EnableAccessAuditing = true };
        _requestContext.CorrelationId.Returns("corr-123");
        _requestContext.UserId.Returns("user-1");
        _auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var sut = new AuditedSecretWriterDecorator(
            _inner, _auditStore, _requestContext, options,
            NullLogger<AuditedSecretWriterDecorator>.Instance);

        _inner.SetSecretAsync("test-secret", "value", Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var result = await sut.SetSecretAsync("test-secret", "value");

        result.IsRight.ShouldBeTrue();
        await _auditStore.Received(1).RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }

    #endregion
}
