using System.Globalization;
using System.Reflection;

using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;
using Encina.UnitTests.Support;

using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Marten.GDPR;

/// <summary>
/// Unit tests for <see cref="CryptoShreddingAutoRegistrationHostedService"/>: skip paths, the
/// validation rules of the startup scan and the redacted warning for assemblies that fail to load (#1557).
/// </summary>
[Trait("Category", "Unit")]
public sealed class CryptoShreddingAutoRegistrationHostedServiceTests
{
    private const string Sentinel = "SENTINEL-LOAD-FAILURE-77f";

    private static CryptoShreddingAutoRegistrationHostedService CreateSut(
        FakeLogger<CryptoShreddingAutoRegistrationHostedService> logger,
        bool autoRegister,
        params Assembly[] assemblies) =>
        new(
            new CryptoShreddingAutoRegistrationDescriptor(assemblies),
            Options.Create(new CryptoShreddingOptions { AutoRegisterFromAttributes = autoRegister }),
            logger);

    [Fact]
    public async Task StartAsync_Disabled_SkipsScan()
    {
        var logger = new FakeLogger<CryptoShreddingAutoRegistrationHostedService>();
        var sut = CreateSut(logger, autoRegister: false, new FakeAssembly(typeof(MissingPersonalData)));

        await sut.StartAsync(CancellationToken.None);

        logger.Collector.GetSnapshot().ShouldAllBe(r => r.Level == LogLevel.Debug);
    }

    [Fact]
    public async Task StartAsync_NoAssemblies_SkipsScan()
    {
        var logger = new FakeLogger<CryptoShreddingAutoRegistrationHostedService>();
        var sut = CreateSut(logger, autoRegister: true);

        await sut.StartAsync(CancellationToken.None);

        logger.Collector.GetSnapshot().ShouldAllBe(r => r.Level == LogLevel.Debug);
    }

    [Fact]
    public async Task StartAsync_ValidTypes_CompletesWithoutErrors()
    {
        var logger = new FakeLogger<CryptoShreddingAutoRegistrationHostedService>();
        var sut = CreateSut(logger, autoRegister: true, new FakeAssembly(typeof(ValidEvent), typeof(PlainType)));

        await sut.StartAsync(CancellationToken.None);

        logger.Collector.GetSnapshot().ShouldNotContain(r => r.Level == LogLevel.Error);
    }

    [Fact]
    public async Task StartAsync_MissingPersonalData_ThrowsAndLogsError()
    {
        var logger = new FakeLogger<CryptoShreddingAutoRegistrationHostedService>();
        var sut = CreateSut(logger, autoRegister: true, new FakeAssembly(typeof(MissingPersonalData)));

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));

        ex.Message.ShouldContain("1 validation error(s)");
        ex.Message.ShouldContain("missing [PersonalData]");
        logger.Collector.GetSnapshot().Count(r => r.Level == LogLevel.Error).ShouldBe(1);
    }

    [Fact]
    public async Task StartAsync_UnknownSubjectIdProperty_ThrowsWithReason()
    {
        var logger = new FakeLogger<CryptoShreddingAutoRegistrationHostedService>();
        var sut = CreateSut(logger, autoRegister: true, new FakeAssembly(typeof(UnknownSubject)));

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));

        ex.Message.ShouldContain("does not exist");
    }

    [Fact]
    public async Task StartAsync_NonStringSubjectIdProperty_ThrowsWithReason()
    {
        var logger = new FakeLogger<CryptoShreddingAutoRegistrationHostedService>();
        var sut = CreateSut(logger, autoRegister: true, new FakeAssembly(typeof(NonStringSubject)));

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));

        ex.Message.ShouldContain("instead of 'string'");
    }

    [Fact]
    public async Task StartAsync_SeveralInvalidTypes_ReportsEveryError()
    {
        var logger = new FakeLogger<CryptoShreddingAutoRegistrationHostedService>();
        var sut = CreateSut(
            logger,
            autoRegister: true,
            new FakeAssembly(typeof(MissingPersonalData), typeof(UnknownSubject), typeof(NonStringSubject)));

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));

        ex.Message.ShouldContain(3.ToString(CultureInfo.InvariantCulture) + " validation error(s)");
    }

    [Fact]
    public async Task StartAsync_TypeLoadFailure_LogsRedactedWarningAndScansLoadedTypes()
    {
        var logger = new FakeLogger<CryptoShreddingAutoRegistrationHostedService>();
        var loadFailure = new ReflectionTypeLoadException(
            [typeof(ValidEvent), null],
            [new InvalidOperationException(Sentinel)],
            Sentinel);
        var sut = CreateSut(logger, autoRegister: true, new FakeAssembly(loadFailure));

        await sut.StartAsync(CancellationToken.None);

        logger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Warning);
        RedactedExceptionLogAssert.LoggedOnlyRedacted(logger, Sentinel);
    }

    public sealed class ValidEvent
    {
        public string UserId { get; set; } = string.Empty;

        [PersonalData]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class PlainType
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class MissingPersonalData
    {
        public string UserId { get; set; } = string.Empty;

        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class UnknownSubject
    {
        [PersonalData]
        [CryptoShredded(SubjectIdProperty = "Nope")]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class NonStringSubject
    {
        public int UserId { get; set; }

        [PersonalData]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    private sealed class FakeAssembly : Assembly
    {
        private readonly Type[] _types;
        private readonly ReflectionTypeLoadException? _failure;

        public FakeAssembly(params Type[] types) => _types = types;

        public FakeAssembly(ReflectionTypeLoadException failure)
        {
            _types = [];
            _failure = failure;
        }

        public override Type[] GetTypes() => _failure is null ? _types : throw _failure;

        public override AssemblyName GetName() => new("FakeAssembly");
    }
}
