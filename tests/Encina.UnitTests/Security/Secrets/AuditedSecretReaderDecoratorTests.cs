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

public sealed class AuditedSecretReaderDecoratorTests
{
    private readonly ISecretReader _innerReader;
    private readonly IAuditStore _auditStore;
    private readonly IRequestContext _requestContext;
    private readonly ILogger<AuditedSecretReaderDecorator> _logger;

    public AuditedSecretReaderDecoratorTests()
    {
        _innerReader = Substitute.For<ISecretReader>();
        _auditStore = Substitute.For<IAuditStore>();
        _requestContext = Substitute.For<IRequestContext>();
        _logger = Substitute.For<ILogger<AuditedSecretReaderDecorator>>();

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

        var act = () => new AuditedSecretReaderDecorator(null!, _auditStore, _requestContext, options, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("inner");
    }

    [Fact]
    public void Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        var options = CreateOptions(true);

        var act = () => new AuditedSecretReaderDecorator(_innerReader, null!, _requestContext, options, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("auditStore");
    }

    [Fact]
    public void Constructor_NullRequestContext_ThrowsArgumentNullException()
    {
        var options = CreateOptions(true);

        var act = () => new AuditedSecretReaderDecorator(_innerReader, _auditStore, null!, options, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("requestContext");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new AuditedSecretReaderDecorator(_innerReader, _auditStore, _requestContext, null!, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var options = CreateOptions(true);

        var act = () => new AuditedSecretReaderDecorator(_innerReader, _auditStore, _requestContext, options, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetSecretAsync - Auditing Disabled

    [Fact]
    public async Task GetSecretAsync_AuditingDisabled_PassesThroughToInner()
    {
        var decorator = CreateDecorator(enableAuditing: false);
        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("value"));

        var result = await decorator.GetSecretAsync("key");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("value"));
        await _auditStore.DidNotReceive().RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetSecretAsync - Auditing Enabled

    [Fact]
    public async Task GetSecretAsync_AuditingEnabled_RecordsAuditEntry()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerReader.GetSecretAsync("api-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("secret-value"));

        await decorator.GetSecretAsync("api-key");

        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.Action == "SecretAccess" &&
                e.EntityType == "Secret" &&
                e.EntityId == "api-key" &&
                e.Outcome == AuditOutcome.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretAsync_AuditingEnabled_ReturnsInnerResult()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("expected"));

        var result = await decorator.GetSecretAsync("key");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("expected"));
    }

    [Fact]
    public async Task GetSecretAsync_AuditingEnabled_FailedRead_RecordsFailureOutcome()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerReader.GetSecretAsync("missing-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.NotFound("missing-key")));

        var result = await decorator.GetSecretAsync("missing-key");

        result.IsLeft.Should().BeTrue();
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.Outcome == AuditOutcome.Failure &&
                e.EntityId == "missing-key"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretAsync_AuditingEnabled_SetsUserAndTenant()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("value"));

        await decorator.GetSecretAsync("key");

        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.UserId == "test-user" &&
                e.TenantId == "test-tenant"),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Audit Failure Resilience

    [Fact]
    public async Task GetSecretAsync_AuditStoreFails_StillReturnsSecretValue()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("value"));

        _auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(
                SecretsErrors.AuditFailed("key")));

        var result = await decorator.GetSecretAsync("key");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("value"));
    }

    [Fact]
    public async Task GetSecretAsync_AuditStoreThrows_StillReturnsSecretValue()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("value"));

        _auditStore.When(x => x.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("audit store crashed"));

        var result = await decorator.GetSecretAsync("key");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("value"));
    }

    #endregion

    #region GetSecretAsync<T> - Typed

    [Fact]
    public async Task GetSecretAsync_Typed_AuditingDisabled_PassesThrough()
    {
        var decorator = CreateDecorator(enableAuditing: false);
        var expected = new TestConfig { Host = "localhost" };
        _innerReader.GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(expected));

        var result = await decorator.GetSecretAsync<TestConfig>("config");

        result.IsRight.Should().BeTrue();
        await _auditStore.DidNotReceive().RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretAsync_Typed_AuditingEnabled_RecordsAuditEntry()
    {
        var decorator = CreateDecorator(enableAuditing: true);
        var expected = new TestConfig { Host = "localhost" };
        _innerReader.GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(expected));

        await decorator.GetSecretAsync<TestConfig>("config");

        await _auditStore.Received(1).RecordAsync(
            Arg.Is<AuditEntry>(e =>
                e.Action == "SecretAccess" &&
                e.EntityType == "Secret" &&
                e.EntityId == "config" &&
                e.Outcome == AuditOutcome.Success),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helpers

    private AuditedSecretReaderDecorator CreateDecorator(bool enableAuditing) =>
        new(_innerReader, _auditStore, _requestContext,
            CreateOptions(enableAuditing), _logger);

    private static SecretsOptions CreateOptions(bool enableAuditing) =>
        new() { EnableAccessAuditing = enableAuditing };

    private sealed class TestConfig
    {
        public string Host { get; set; } = "";
    }

    #endregion
}
