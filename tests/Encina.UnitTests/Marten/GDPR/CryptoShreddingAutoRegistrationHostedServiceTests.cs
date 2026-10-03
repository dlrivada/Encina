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
    public async Task StartAsync_UnsupportedSubjectIdType_ThrowsNamingTypeAndProperty()
    {
        var logger = new FakeLogger<CryptoShreddingAutoRegistrationHostedService>();
        var sut = CreateSut(logger, autoRegister: true, new FakeAssembly(typeof(UnsupportedSubject)));

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));

        ex.Message.ShouldContain("SubjectIdProperty='UserId'");
        ex.Message.ShouldContain("'Double'");
        ex.Message.ShouldContain(nameof(UnsupportedSubject));
        ex.Message.ShouldContain("Supported subject-id types");
    }

    [Fact]
    public async Task StartAsync_NullableUnsupportedSubjectIdType_NamesTheUnderlyingType()
    {
        var logger = new FakeLogger<CryptoShreddingAutoRegistrationHostedService>();
        var sut = CreateSut(logger, autoRegister: true, new FakeAssembly(typeof(NullableDateSubject)));

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));

        ex.Message.ShouldContain("'DateTime?'");
        ex.Message.ShouldNotContain("Nullable`1");
    }

    [Fact]
    public async Task StartAsync_WriteOnlySubjectIdProperty_Throws()
    {
        var logger = new FakeLogger<CryptoShreddingAutoRegistrationHostedService>();
        var sut = CreateSut(logger, autoRegister: true, new FakeAssembly(typeof(WriteOnlySubject)));

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));

        ex.Message.ShouldContain("it has no getter");
    }

    [Fact]
    public async Task StartAsync_NonStringEncryptedProperty_Throws()
    {
        var logger = new FakeLogger<CryptoShreddingAutoRegistrationHostedService>();
        var sut = CreateSut(logger, autoRegister: true, new FakeAssembly(typeof(NonStringEncrypted)));

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));

        ex.Message.ShouldContain("Only string properties can be encrypted");
        ex.Message.ShouldContain("'Int32'");
    }

    [Fact]
    public async Task StartAsync_GetterOnlyEncryptedProperty_ThrowsNamingPropertyAndType()
    {
        // #1646: the serializer cannot overwrite a getter-only property with its ciphertext.
        var logger = new FakeLogger<CryptoShreddingAutoRegistrationHostedService>();
        var sut = CreateSut(logger, autoRegister: true, new FakeAssembly(typeof(GetterOnlyEncrypted)));

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));

        ex.Message.ShouldContain("1 validation error(s)");
        ex.Message.ShouldContain($"Property '{nameof(GetterOnlyEncrypted.Email)}'");
        ex.Message.ShouldContain(typeof(GetterOnlyEncrypted).FullName!);
        ex.Message.ShouldContain("setter");
    }

    [Fact]
    public async Task StartAsync_StructEvent_ThrowsNamingPropertyAndType()
    {
        // #1646 review: a setter compiled for a struct never reaches the boxed event.
        var logger = new FakeLogger<CryptoShreddingAutoRegistrationHostedService>();
        var sut = CreateSut(logger, autoRegister: true, new FakeAssembly(typeof(StructEncrypted)));

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));

        ex.Message.ShouldContain("1 validation error(s)");
        ex.Message.ShouldContain(typeof(StructEncrypted).FullName!);
        ex.Message.ShouldContain("struct");
    }

    [Fact]
    public async Task StartAsync_PositionalRecord_CompletesWithoutErrors()
    {
        var logger = new FakeLogger<CryptoShreddingAutoRegistrationHostedService>();
        var sut = CreateSut(logger, autoRegister: true, new FakeAssembly(typeof(PositionalRecordEvent)));

        await sut.StartAsync(CancellationToken.None);

        logger.Collector.GetSnapshot().ShouldNotContain(r => r.Level == LogLevel.Error);
    }

    [Fact]
    public async Task StartAsync_SupportedSubjectIdTypes_CompletesWithoutErrors()
    {
        var logger = new FakeLogger<CryptoShreddingAutoRegistrationHostedService>();
        var sut = CreateSut(
            logger,
            autoRegister: true,
            new FakeAssembly(typeof(GuidSubject), typeof(IntSubject), typeof(NullableLongSubject), typeof(WrappedSubject)));

        await sut.StartAsync(CancellationToken.None);

        logger.Collector.GetSnapshot().ShouldNotContain(r => r.Level == LogLevel.Error);
    }

    [Fact]
    public async Task StartAsync_SeveralInvalidTypes_ReportsEveryError()
    {
        var logger = new FakeLogger<CryptoShreddingAutoRegistrationHostedService>();
        var sut = CreateSut(
            logger,
            autoRegister: true,
            new FakeAssembly(typeof(MissingPersonalData), typeof(UnknownSubject), typeof(UnsupportedSubject)));

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

    public sealed class UnsupportedSubject
    {
        public double UserId { get; set; }

        [PersonalData]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class NullableDateSubject
    {
        public DateTime? UserId { get; set; }

        [PersonalData]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class WriteOnlySubject
    {
        private Guid _userId;

        public Guid UserId
        {
            set => _userId = value;
        }

        public Guid StoredId => _userId;

        [PersonalData]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class NonStringEncrypted
    {
        public string UserId { get; set; } = string.Empty;

        [PersonalData]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public int Age { get; set; }
    }

    public sealed class GetterOnlyEncrypted(string userId, string email)
    {
        public string UserId { get; } = userId;

        [PersonalData]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; } = email;
    }

    public record struct StructEncrypted
    {
        public string UserId { get; set; }

        [PersonalData]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; }
    }

    public sealed record PositionalRecordEvent(
        string UserId,
        [property: PersonalData, CryptoShredded(SubjectIdProperty = nameof(PositionalRecordEvent.UserId))] string Email);

    public sealed class GuidSubject
    {
        public Guid UserId { get; set; }

        [PersonalData]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class IntSubject
    {
        public int UserId { get; set; }

        [PersonalData]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class NullableLongSubject
    {
        public long? UserId { get; set; }

        [PersonalData]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    public sealed record SubjectKey(Guid Value);

    public sealed class WrappedSubject
    {
        public SubjectKey UserId { get; set; } = new(Guid.Empty);

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
