#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Audit;
using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Auditing;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Security.Secrets;

/// <summary>
/// The audit entries of the secret decorators persist only the error code, never the error message (#1557).
/// </summary>
public sealed class SecretAuditRecorderRedactionTests
{
    private const string Sentinel = "SENTINEL-secret-error-message";
    private const string Code = "secrets.sentinel.code";

    private readonly IOperationAuditStore _auditStore = Substitute.For<IOperationAuditStore>();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRequestContextAccessor _accessor = Substitute.For<IRequestContextAccessor>();
    private readonly SecretsOptions _options = new() { EnableAccessAuditing = true };

    public SecretAuditRecorderRedactionTests()
    {
        _scopeFactory = new ServiceCollection()
            .AddSingleton(_auditStore)
            .BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();
        _auditStore.RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));
    }

    private static EncinaError SentinelError() => EncinaErrors.Create(Code, Sentinel);

    private async Task AssertRecordedErrorIsTheCode()
    {
        var calls = _auditStore.ReceivedCalls().ToList();
        calls.Count.ShouldBe(1);
        var entry = (OperationAuditEntry)calls[0].GetArguments()[0]!;
        entry.Outcome.ShouldBe(AuditOutcome.Failure);
        entry.ErrorMessage.ShouldBe(Code);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Reader_FailedRead_RecordsTheErrorCodeNotTheMessage()
    {
        var inner = Substitute.For<ISecretReader>();
        inner.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(SentinelError()));
        var sut = new AuditedSecretReaderDecorator(
            inner, _scopeFactory, _accessor, _options, Substitute.For<ILogger<AuditedSecretReaderDecorator>>());

        await sut.GetSecretAsync("key");

        await AssertRecordedErrorIsTheCode();
    }

    [Fact]
    public async Task Reader_AuditStoreRejectsTheEntry_LogsTheStoreErrorCodeNotItsMessage()
    {
        var inner = Substitute.For<ISecretReader>();
        inner.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("value"));
        _auditStore.RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(EncinaErrors.Create("audit.store.code", Sentinel)));
        var logger = new Microsoft.Extensions.Logging.Testing.FakeLogger<AuditedSecretReaderDecorator>();
        var sut = new AuditedSecretReaderDecorator(inner, _scopeFactory, _accessor, _options, logger);

        await sut.GetSecretAsync("key");

        var records = logger.Collector.GetSnapshot();
        records.ShouldContain(r => r.Message.Contains("audit.store.code", StringComparison.Ordinal));
        records.ShouldAllBe(r => !r.Message.Contains(Sentinel, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Writer_FailedWrite_RecordsTheErrorCodeNotTheMessage()
    {
        var inner = Substitute.For<ISecretWriter>();
        inner.SetSecretAsync("key", "value", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(SentinelError()));
        var sut = new AuditedSecretWriterDecorator(
            inner, _scopeFactory, _accessor, _options, Substitute.For<ILogger<AuditedSecretWriterDecorator>>());

        await sut.SetSecretAsync("key", "value");

        await AssertRecordedErrorIsTheCode();
    }

    [Fact]
    public async Task Rotator_FailedRotation_RecordsTheErrorCodeNotTheMessage()
    {
        var inner = Substitute.For<ISecretRotator>();
        inner.RotateSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(SentinelError()));
        var sut = new AuditedSecretRotatorDecorator(
            inner, _scopeFactory, _accessor, _options, Substitute.For<ILogger<AuditedSecretRotatorDecorator>>());

        await sut.RotateSecretAsync("key");

        await AssertRecordedErrorIsTheCode();
    }
}
