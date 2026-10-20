using System.Buffers;
using System.Data.Common;

using Encina.Marten.GDPR;

using Marten;
using Marten.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Npgsql;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Marten.GDPR;

/// <summary>
/// Guard clause tests for <see cref="CryptoShredderSerializer"/> and <see cref="CryptoShredderSerializerFactory"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "Marten")]
public sealed class CryptoShredderSerializerGuardTests
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly ILogger<CryptoShredderSerializer> _logger = NullLogger<CryptoShredderSerializer>.Instance;

    private static SystemTextJsonSerializer NewInner() => (SystemTextJsonSerializer)new StoreOptions().Serializer();

    [Fact]
    public void Constructor_NullInnerSerializer_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() => new CryptoShredderSerializer(null!, _scopeFactory, _logger)).ParamName.ShouldBe("inner");

    [Fact]
    public void Constructor_NullScopeFactory_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() => new CryptoShredderSerializer(NewInner(), null!, _logger)).ParamName.ShouldBe("scopeFactory");

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() => new CryptoShredderSerializer(NewInner(), _scopeFactory, null!)).ParamName.ShouldBe("logger");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankAnonymizedPlaceholder_ThrowsArgumentException(string? placeholder) =>
        Should.Throw<ArgumentException>(() => new CryptoShredderSerializer(NewInner(), _scopeFactory, _logger, placeholder!))
            .ParamName.ShouldBe("anonymizedPlaceholder");

    [Fact]
    public void WriteMembers_NullArguments_ThrowArgumentNullException()
    {
        var sut = new CryptoShredderSerializer(NewInner(), _scopeFactory, _logger);
        var buffer = new ArrayBufferWriter<byte>();

        Should.Throw<ArgumentNullException>(() => sut.ToJsonWithTypes(null!)).ParamName.ShouldBe("document");
        Should.Throw<ArgumentNullException>(() => sut.WriteTo(null!, new object())).ParamName.ShouldBe("writer");
        Should.Throw<ArgumentNullException>(() => sut.WriteToCleanJson(null!, new object())).ParamName.ShouldBe("writer");
        Should.Throw<ArgumentNullException>(() => sut.WriteToJsonWithTypes(null!, new object())).ParamName.ShouldBe("writer");
        Should.Throw<ArgumentNullException>(() => sut.WriteToJsonWithTypes(buffer, null!)).ParamName.ShouldBe("value");
        Should.Throw<ArgumentNullException>(() => sut.WriteToParameter((DbParameter)null!, new object())).ParamName.ShouldBe("parameter");
        Should.Throw<ArgumentNullException>(() => sut.WriteToParameter((NpgsqlParameter)null!, new object())).ParamName.ShouldBe("parameter");
    }

    [Fact]
    public void FactoryApply_NullArguments_ThrowArgumentException()
    {
        Should.Throw<ArgumentNullException>(() => CryptoShredderSerializerFactory.Apply(null!, _scopeFactory, _logger)).ParamName.ShouldBe("options");
        Should.Throw<ArgumentNullException>(() => CryptoShredderSerializerFactory.Apply(new StoreOptions(), null!, _logger)).ParamName.ShouldBe("scopeFactory");
        Should.Throw<ArgumentNullException>(() => CryptoShredderSerializerFactory.Apply(new StoreOptions(), _scopeFactory, null!)).ParamName.ShouldBe("logger");
        Should.Throw<ArgumentException>(() => CryptoShredderSerializerFactory.Apply(new StoreOptions(), _scopeFactory, _logger, " ")).ParamName.ShouldBe("anonymizedPlaceholder");
    }

    [Fact]
    public void FactoryApply_ValidArguments_WrapsTheSerializer()
    {
        var options = new StoreOptions();

        CryptoShredderSerializerFactory.Apply(options, _scopeFactory, _logger);

        options.Serializer().ShouldBeOfType<CryptoShredderSerializer>();
    }
}
