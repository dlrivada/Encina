using Encina.Security.Audit;
using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Auditing;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.GuardTests.Security.Secrets.Auditing;

/// <summary>
/// Guard tests for <see cref="AuditedSecretReaderDecorator"/> including constructor and method-level guards.
/// </summary>
public sealed class AuditedSecretReaderDecoratorGuardTests
{
    private readonly ISecretReader _inner = Substitute.For<ISecretReader>();
    private readonly IAuditStore _auditStore = Substitute.For<IAuditStore>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly SecretsOptions _options = new();

    #region Constructor Guards

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretReaderDecorator(
            null!, _auditStore, _requestContext, _options,
            NullLogger<AuditedSecretReaderDecorator>.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretReaderDecorator(
            _inner, null!, _requestContext, _options,
            NullLogger<AuditedSecretReaderDecorator>.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("auditStore");
    }

    [Fact]
    public void Constructor_NullRequestContext_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretReaderDecorator(
            _inner, _auditStore, null!, _options,
            NullLogger<AuditedSecretReaderDecorator>.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("requestContext");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretReaderDecorator(
            _inner, _auditStore, _requestContext, null!,
            NullLogger<AuditedSecretReaderDecorator>.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretReaderDecorator(
            _inner, _auditStore, _requestContext, _options, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region Method Guards - GetSecretAsync delegates

    [Fact]
    public async Task GetSecretAsync_AuditingDisabled_DelegatesToInner()
    {
        // Arrange
        var options = new SecretsOptions { EnableAccessAuditing = false };
        var sut = new AuditedSecretReaderDecorator(
            _inner, _auditStore, _requestContext, options,
            NullLogger<AuditedSecretReaderDecorator>.Instance);

        _inner.GetSecretAsync("test-secret", Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, string>.Right("value"));

        // Act
        var result = await sut.GetSecretAsync("test-secret");

        // Assert
        result.IsRight.ShouldBeTrue();
        await _inner.Received(1).GetSecretAsync("test-secret", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretAsync_AuditingEnabled_RecordsEntry()
    {
        // Arrange
        var options = new SecretsOptions { EnableAccessAuditing = true };
        _requestContext.CorrelationId.Returns("corr-123");
        _requestContext.UserId.Returns("user-1");
        _auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        var sut = new AuditedSecretReaderDecorator(
            _inner, _auditStore, _requestContext, options,
            NullLogger<AuditedSecretReaderDecorator>.Instance);

        _inner.GetSecretAsync("test-secret", Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, string>.Right("value"));

        // Act
        var result = await sut.GetSecretAsync("test-secret");

        // Assert
        result.IsRight.ShouldBeTrue();
        await _auditStore.Received(1).RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretAsync_GenericT_AuditingDisabled_DelegatesToInner()
    {
        // Arrange
        var options = new SecretsOptions { EnableAccessAuditing = false };
        var sut = new AuditedSecretReaderDecorator(
            _inner, _auditStore, _requestContext, options,
            NullLogger<AuditedSecretReaderDecorator>.Instance);

        _inner.GetSecretAsync<TestSecretValue>("test-secret", Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, TestSecretValue>.Right(new TestSecretValue { Key = "k" }));

        // Act
        var result = await sut.GetSecretAsync<TestSecretValue>("test-secret");

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    private sealed class TestSecretValue
    {
        public string Key { get; init; } = string.Empty;
    }
}
