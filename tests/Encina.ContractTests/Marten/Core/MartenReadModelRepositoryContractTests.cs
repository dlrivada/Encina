using Encina.Marten.Projections;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Marten.Core;

/// <summary>
/// Behavioral contract tests for <see cref="MartenReadModelRepository{TReadModel}"/>
/// verifying IReadModelRepository contract compliance.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Provider", "Marten")]
public sealed class MartenReadModelRepositoryContractTests
{
    #region Structural Contracts

    [Fact]
    public void MartenReadModelRepository_IsSealed()
    {
        typeof(MartenReadModelRepository<>).IsSealed.ShouldBeTrue();
    }

    [Fact]
    public void MartenReadModelRepository_ImplementsIReadModelRepository()
    {
        typeof(MartenReadModelRepository<>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadModelRepository<>));
    }

    [Fact]
    public void IReadModelRepository_HasExistsAsync()
    {
        var methods = typeof(IReadModelRepository<>).GetMethods();
        methods.ShouldContain(m => m.Name == "ExistsAsync");
    }

    [Fact]
    public void IReadModelRepository_HasCountAsync()
    {
        var methods = typeof(IReadModelRepository<>).GetMethods();
        methods.ShouldContain(m => m.Name == "CountAsync");
    }

    [Fact]
    public void IReadModelRepository_HasDeleteAllAsync()
    {
        var methods = typeof(IReadModelRepository<>).GetMethods();
        methods.ShouldContain(m => m.Name == "DeleteAllAsync");
    }

    [Fact]
    public void IReadModelRepository_HasStoreManyAsync()
    {
        var methods = typeof(IReadModelRepository<>).GetMethods();
        methods.ShouldContain(m => m.Name == "StoreManyAsync");
    }

    #endregion

    #region Method Guards Contract

    [Fact]
    public async Task StoreAsync_NullReadModel_ThrowsArgumentNull()
    {
        var repo = CreateRepository();
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.StoreAsync(null!));
    }

    [Fact]
    public async Task StoreManyAsync_NullReadModels_ThrowsArgumentNull()
    {
        var repo = CreateRepository();
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.StoreManyAsync(null!));
    }

    [Fact]
    public async Task GetByIdsAsync_NullIds_ThrowsArgumentNull()
    {
        var repo = CreateRepository();
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.GetByIdsAsync(null!));
    }

    [Fact]
    public async Task QueryAsync_NullPredicate_ThrowsArgumentNull()
    {
        var repo = CreateRepository();
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.QueryAsync(null!));
    }

    [Fact]
    public async Task CountAsync_WithPredicate_NullPredicate_ThrowsArgumentNull()
    {
        var repo = CreateRepository();
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.CountAsync((Func<IQueryable<ContractReadModel>, IQueryable<ContractReadModel>>)null!));
    }

    #endregion

    #region Helpers

    private static MartenReadModelRepository<ContractReadModel> CreateRepository()
    {
        return new MartenReadModelRepository<ContractReadModel>(
            NSubstitute.Substitute.For<IDocumentSession>(),
            NullLogger<MartenReadModelRepository<ContractReadModel>>.Instance);
    }

    #endregion

    public sealed class ContractReadModel : IReadModel
    {
        public Guid Id { get; set; }
    }
}
