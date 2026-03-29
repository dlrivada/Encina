using Encina.Marten.Projections;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Encina.UnitTests.Marten.Projections;

public class MartenReadModelRepositoryTests
{
    private readonly IDocumentSession _session;
    private readonly ILogger<MartenReadModelRepository<TestReadModel>> _logger;

    public MartenReadModelRepositoryTests()
    {
        _session = Substitute.For<IDocumentSession>();
        _logger = NullLogger<MartenReadModelRepository<TestReadModel>>.Instance;
    }

    private MartenReadModelRepository<TestReadModel> CreateSut()
    {
        return new MartenReadModelRepository<TestReadModel>(_session, _logger);
    }

    // Constructor null guard tests

    [Fact]
    public void Constructor_NullSession_ThrowsArgumentNullException()
    {
        var act = () => new MartenReadModelRepository<TestReadModel>(null!, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("session");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new MartenReadModelRepository<TestReadModel>(_session, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    // GetByIdAsync tests

    [Fact]
    public async Task GetByIdAsync_ReadModelFound_ReturnsRight()
    {
        // Arrange
        var id = Guid.NewGuid();
        var readModel = new TestReadModel { Id = id, Name = "test" };

        _session.LoadAsync<TestReadModel>(id, Arg.Any<CancellationToken>())
            .Returns(readModel);

        var sut = CreateSut();

        // Act
        var result = await sut.GetByIdAsync(id);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: rm => rm.Name.ShouldBe("test"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetByIdAsync_ReadModelNotFound_ReturnsLeft()
    {
        // Arrange
        var id = Guid.NewGuid();
        _session.LoadAsync<TestReadModel>(id, Arg.Any<CancellationToken>())
            .Returns((TestReadModel?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetByIdAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_ExceptionThrown_ReturnsLeft()
    {
        // Arrange
        var id = Guid.NewGuid();
        _session.LoadAsync<TestReadModel>(id, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var sut = CreateSut();

        // Act
        var result = await sut.GetByIdAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // GetByIdsAsync tests

    [Fact]
    public async Task GetByIdsAsync_NullIds_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.GetByIdsAsync(null!);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task GetByIdsAsync_ExceptionThrown_ReturnsLeft()
    {
        // Arrange
        var ids = new[] { Guid.NewGuid() };
        _session.LoadManyAsync<TestReadModel>(Arg.Any<CancellationToken>(), Arg.Any<Guid[]>())
            .ThrowsAsync(new InvalidOperationException("error"));

        var sut = CreateSut();

        // Act
        var result = await sut.GetByIdsAsync(ids);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // QueryAsync tests

    [Fact]
    public async Task QueryAsync_NullPredicate_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.QueryAsync(null!);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    // StoreAsync tests

    [Fact]
    public async Task StoreAsync_NullReadModel_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.StoreAsync(null!);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task StoreAsync_ValidReadModel_StoresAndSaves()
    {
        // Arrange
        var readModel = new TestReadModel { Id = Guid.NewGuid(), Name = "test" };
        var sut = CreateSut();

        // Act
        var result = await sut.StoreAsync(readModel);

        // Assert
        result.IsRight.ShouldBeTrue();
        _session.Received(1).Store(readModel);
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StoreAsync_ExceptionThrown_ReturnsLeft()
    {
        // Arrange
        var readModel = new TestReadModel { Id = Guid.NewGuid(), Name = "test" };
        _session.When(s => s.Store(Arg.Any<TestReadModel>()))
            .Do(_ => throw new InvalidOperationException("error"));

        var sut = CreateSut();

        // Act
        var result = await sut.StoreAsync(readModel);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // StoreManyAsync tests

    [Fact]
    public async Task StoreManyAsync_NullReadModels_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.StoreManyAsync(null!);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task StoreManyAsync_ExceptionThrown_ReturnsLeft()
    {
        // Arrange
        var readModels = new[] { new TestReadModel { Id = Guid.NewGuid(), Name = "test" } };
        _session.When(s => s.Store<TestReadModel>(Arg.Any<TestReadModel[]>()))
            .Do(_ => throw new InvalidOperationException("error"));

        var sut = CreateSut();

        // Act
        var result = await sut.StoreManyAsync(readModels);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // DeleteAsync tests

    [Fact]
    public async Task DeleteAsync_ValidId_DeletesAndSaves()
    {
        // Arrange
        var id = Guid.NewGuid();
        var sut = CreateSut();

        // Act
        var result = await sut.DeleteAsync(id);

        // Assert
        result.IsRight.ShouldBeTrue();
        _session.Received(1).Delete<TestReadModel>(id);
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ExceptionThrown_ReturnsLeft()
    {
        // Arrange
        var id = Guid.NewGuid();
        _session.When(s => s.Delete<TestReadModel>(id))
            .Do(_ => throw new InvalidOperationException("error"));

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // DeleteAllAsync tests

    [Fact]
    public async Task DeleteAllAsync_ExceptionThrown_ReturnsLeft()
    {
        // Arrange
        _session.Query<TestReadModel>()
            .Throws(new InvalidOperationException("error"));

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteAllAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ExistsAsync tests

    [Fact]
    public async Task ExistsAsync_ExceptionThrown_ReturnsLeft()
    {
        // Arrange
        var id = Guid.NewGuid();
        _session.Query<TestReadModel>()
            .Throws(new InvalidOperationException("error"));

        var sut = CreateSut();

        // Act
        var result = await sut.ExistsAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // CountAsync tests

    [Fact]
    public async Task CountAsync_ExceptionThrown_ReturnsLeft()
    {
        // Arrange
        _session.Query<TestReadModel>()
            .Throws(new InvalidOperationException("error"));

        var sut = CreateSut();

        // Act
        var result = await sut.CountAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // CountAsync with predicate tests

    [Fact]
    public async Task CountAsync_WithNullPredicate_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        Func<IQueryable<TestReadModel>, IQueryable<TestReadModel>>? predicate = null;
        var act = () => sut.CountAsync(predicate!);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task CountAsync_WithPredicate_ExceptionThrown_ReturnsLeft()
    {
        // Arrange
        _session.Query<TestReadModel>()
            .Throws(new InvalidOperationException("error"));

        var sut = CreateSut();

        // Act
        var result = await sut.CountAsync(q => q.Where(r => r.Name == "test"));

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // Test types

    public sealed class TestReadModel : IReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
