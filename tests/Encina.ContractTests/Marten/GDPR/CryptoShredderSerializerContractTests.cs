using System.Buffers;
using System.Text;

using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;

using Marten;
using Marten.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Encina.ContractTests.Marten.GDPR;

/// <summary>
/// The <see cref="ISerializer"/> contract of <see cref="CryptoShredderSerializer"/> (#1698): documents without
/// personal data are written byte-identically to Marten's serializer, the Weasel settings delegate, and documents
/// with nested personal data round-trip through every read member.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Provider", "Marten")]
public sealed class CryptoShredderSerializerContractTests : IDisposable
{
    private readonly ServiceProvider _services;
    private readonly ISerializer _plain = new StoreOptions().Serializer();
    private readonly CryptoShredderSerializer _sut;

    public CryptoShredderSerializerContractTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISubjectKeyProvider>(new InMemorySubjectKeyProvider(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance));
        services.AddSingleton<IForgottenSubjectHandler>(new DefaultForgottenSubjectHandler(NullLogger<DefaultForgottenSubjectHandler>.Instance));
        _services = services.BuildServiceProvider();
        _sut = new CryptoShredderSerializer(
            (SystemTextJsonSerializer)new StoreOptions().Serializer(),
            _services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<CryptoShredderSerializer>.Instance);
    }

    public void Dispose() => _services.Dispose();

    [Fact]
    public void NonPiiDocument_EveryWriteMember_IsByteIdenticalToTheInnerSerializer()
    {
        var document = new Plain { Name = "n", Values = [1, 2] };

        _sut.ToJson(document).ShouldBe(_plain.ToJson(document));
        _sut.ToCleanJson(document).ShouldBe(_plain.ToCleanJson(document));
        _sut.ToJsonWithTypes(document).ShouldBe(_plain.ToJsonWithTypes(document));
        Write((s, w) => s.WriteTo(w, document), _sut).ShouldBe(Write((s, w) => s.WriteTo(w, document), _plain));
        Write((s, w) => s.WriteToCleanJson(w, document), _sut).ShouldBe(Write((s, w) => s.WriteToCleanJson(w, document), _plain));
        Write((s, w) => s.WriteToJsonWithTypes(w, document), _sut).ShouldBe(Write((s, w) => s.WriteToJsonWithTypes(w, document), _plain));
    }

    [Fact]
    public async Task PiiDocument_RoundTripsThroughEveryReadMember()
    {
        var bytes = Encoding.UTF8.GetBytes(_sut.ToJson(new Holder { Owner = new Owner { SubjectId = "s", Email = "e@example.com" } }));

        _sut.FromJson<Holder>(new MemoryStream(bytes)).Owner!.Email.ShouldBe("e@example.com");
        ((Holder)_sut.FromJson(typeof(Holder), new MemoryStream(bytes))).Owner!.Email.ShouldBe("e@example.com");
        (await _sut.FromJsonAsync<Holder>(new MemoryStream(bytes))).Owner!.Email.ShouldBe("e@example.com");
        ((Holder)await _sut.FromJsonAsync(typeof(Holder), new MemoryStream(bytes))).Owner!.Email.ShouldBe("e@example.com");
    }

    [Fact]
    public void Settings_DelegateToTheInnerSerializer()
    {
        _sut.EnumStorage.ShouldBe(_plain.EnumStorage);
        _sut.Casing.ShouldBe(_plain.Casing);
        _sut.ValueCasting.ShouldBe(_plain.ValueCasting);
    }

    private static string Write(Action<ISerializer, IBufferWriter<byte>> write, ISerializer serializer)
    {
        var buffer = new ArrayBufferWriter<byte>();
        write(serializer, buffer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public sealed class Plain
    {
        public string? Name { get; set; }

        public List<int> Values { get; set; } = [];
    }

    public sealed class Owner
    {
        public string? SubjectId { get; set; }

        [PersonalData]
        [CryptoShredded(SubjectIdProperty = nameof(SubjectId))]
        public string? Email { get; set; }
    }

    public sealed class Holder
    {
        public Owner? Owner { get; set; }
    }
}
