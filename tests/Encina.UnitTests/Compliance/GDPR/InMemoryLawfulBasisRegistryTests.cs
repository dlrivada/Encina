using Encina.Compliance.GDPR;
using Encina.UnitTests.Compliance.GDPR.Attributes;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Compliance.GDPR;

/// <summary>
/// Unit tests for <see cref="InMemoryLawfulBasisRegistry"/>.
/// </summary>
public class InMemoryLawfulBasisRegistryTests
{
    private readonly InMemoryLawfulBasisRegistry _sut = new();

    private static LawfulBasisRegistration CreateRegistration(
        Type? requestType = null,
        global::Encina.Compliance.GDPR.LawfulBasis basis = LawfulBasis.Contract) => new()
    {
        RequestType = requestType ?? typeof(InMemoryLawfulBasisRegistryTests),
        Basis = basis,
        Purpose = "Test purpose",
        RegisteredAtUtc = DateTimeOffset.UtcNow
    };

    // -- RegisterAsync --

    [Fact]
    public async Task RegisterAsync_ValidRegistration_ShouldSucceed()
    {
        // Arrange
        var registration = CreateRegistration();

        // Act
        var result = await _sut.RegisterAsync(registration);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_DuplicateRequestType_ShouldReturnError()
    {
        // Arrange
        var reg1 = CreateRegistration(typeof(string));
        var reg2 = CreateRegistration(typeof(string));
        await _sut.RegisterAsync(reg1);

        // Act
        var result = await _sut.RegisterAsync(reg2);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task RegisterAsync_DifferentTypes_ShouldBothSucceed()
    {
        // Arrange
        var reg1 = CreateRegistration(typeof(string));
        var reg2 = CreateRegistration(typeof(int));

        // Act
        var result1 = await _sut.RegisterAsync(reg1);
        var result2 = await _sut.RegisterAsync(reg2);

        // Assert
        result1.IsRight.Should().BeTrue();
        result2.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_NullRegistration_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _sut.RegisterAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("registration");
    }

    // -- GetByRequestTypeAsync --

    [Fact]
    public async Task GetByRequestTypeAsync_Registered_ShouldReturnSome()
    {
        // Arrange
        var registration = CreateRegistration(typeof(string));
        await _sut.RegisterAsync(registration);

        // Act
        var result = await _sut.GetByRequestTypeAsync(typeof(string));

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<LawfulBasisRegistration>)result;
        option.IsSome.Should().BeTrue();
        option.IfSome(found =>
        {
            found.RequestType.Should().Be<string>();
            found.Basis.Should().Be(LawfulBasis.Contract);
        });
    }

    [Fact]
    public async Task GetByRequestTypeAsync_NotRegistered_ShouldReturnNone()
    {
        // Act
        var result = await _sut.GetByRequestTypeAsync(typeof(string));

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<LawfulBasisRegistration>)result;
        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetByRequestTypeAsync_NullRequestType_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _sut.GetByRequestTypeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("requestType");
    }

    // -- GetByRequestTypeNameAsync --

    [Fact]
    public async Task GetByRequestTypeNameAsync_Registered_ShouldReturnSome()
    {
        // Arrange
        var registration = CreateRegistration(typeof(string));
        await _sut.RegisterAsync(registration);

        // Act
        var result = await _sut.GetByRequestTypeNameAsync(typeof(string).AssemblyQualifiedName!);

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<LawfulBasisRegistration>)result;
        option.IsSome.Should().BeTrue();
    }

    [Fact]
    public async Task GetByRequestTypeNameAsync_NotRegistered_ShouldReturnNone()
    {
        // Act
        var result = await _sut.GetByRequestTypeNameAsync("NonExistent.Type, SomeAssembly");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<LawfulBasisRegistration>)result;
        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetByRequestTypeNameAsync_NullTypeName_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _sut.GetByRequestTypeNameAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("requestTypeName");
    }

    // -- GetAllAsync --

    [Fact]
    public async Task GetAllAsync_EmptyRegistry_ShouldReturnEmptyList()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var registrations = result.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<LawfulBasisRegistration>)[]);
        registrations.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithRegistrations_ShouldReturnAll()
    {
        // Arrange
        await _sut.RegisterAsync(CreateRegistration(typeof(string)));
        await _sut.RegisterAsync(CreateRegistration(typeof(int)));
        await _sut.RegisterAsync(CreateRegistration(typeof(double)));

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var registrations = result.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<LawfulBasisRegistration>)[]);
        registrations.Should().HaveCount(3);
    }

    // -- AutoRegisterFromAssemblies --

    [Fact]
    public void AutoRegisterFromAssemblies_NullAssemblies_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _sut.AutoRegisterFromAssemblies(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("assemblies");
    }

    [Fact]
    public void AutoRegisterFromAssemblies_AssemblyWithAttributes_ShouldRegisterTypes()
    {
        // Arrange — test assembly contains types decorated with [LawfulBasis]
        var assemblies = new[] { typeof(SampleLawfulBasisDecoratedRequest).Assembly };
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        // Act
        var count = _sut.AutoRegisterFromAssemblies(assemblies, timeProvider);

        // Assert
        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void AutoRegisterFromAssemblies_DuplicateCall_ShouldSkipExisting()
    {
        // Arrange
        var assemblies = new[] { typeof(SampleLawfulBasisDecoratedRequest).Assembly };

        // Act
        var first = _sut.AutoRegisterFromAssemblies(assemblies);
        var second = _sut.AutoRegisterFromAssemblies(assemblies);

        // Assert
        first.Should().BeGreaterThan(0);
        second.Should().Be(0);
    }

    [Fact]
    public void AutoRegisterFromAssemblies_EmptyList_ShouldRegisterZero()
    {
        // Act
        var count = _sut.AutoRegisterFromAssemblies([]);

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void AutoRegisterFromAssemblies_WithTimeProvider_ShouldUseProvidedTime()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedTime);
        var assemblies = new[] { typeof(SampleLawfulBasisDecoratedRequest).Assembly };

        // Act
        _sut.AutoRegisterFromAssemblies(assemblies, timeProvider);

        // Assert
        var result = _sut.GetByRequestTypeAsync(typeof(SampleLawfulBasisDecoratedRequest))
            .AsTask().Result;
        var option = (Option<LawfulBasisRegistration>)result;
        option.IsSome.Should().BeTrue();
        option.IfSome(reg => reg.RegisteredAtUtc.Should().Be(fixedTime));
    }

    // -- Thread safety --

    [Fact]
    public async Task ConcurrentRegistrations_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 100)
            .Select(i => new LawfulBasisRegistration
            {
                RequestType = Type.GetType($"System.Tuple`{(i % 7) + 1}")!,
                Basis = LawfulBasis.Contract,
                RegisteredAtUtc = DateTimeOffset.UtcNow
            })
            .DistinctBy(r => r.RequestType)
            .Select(r => _sut.RegisterAsync(r).AsTask());

        // Act & Assert — should not throw
        await Task.WhenAll(tasks);

        var all = await _sut.GetAllAsync();
        all.IsRight.Should().BeTrue();
    }
}
