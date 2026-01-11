using System.Reflection;
using LanguageExt;
using Shouldly;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests for Repository pattern interfaces.
/// </summary>
public class RepositoryContracts
{
    private readonly Type _readOnlyRepoType = typeof(IReadOnlyRepository<,>);
    private readonly Type _repoType = typeof(IRepository<,>);
    private readonly Type _aggregateRepoType = typeof(IAggregateRepository<,>);
    private readonly Type _pagedResultType = typeof(PagedResult<>);
    private readonly Type _repoErrorType = typeof(RepositoryError);
    private readonly Type _repoExtensionsType = typeof(RepositoryExtensions);
    private readonly Type _entityNotFoundType = typeof(EntityNotFoundException);

    // === IReadOnlyRepository<TEntity, TId> Contract ===

    [Fact]
    public void IReadOnlyRepository_MustBeInterface()
    {
        _readOnlyRepoType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void IReadOnlyRepository_MustBeGenericWithTwoParameters()
    {
        _readOnlyRepoType.IsGenericTypeDefinition.ShouldBeTrue();
        _readOnlyRepoType.GetGenericArguments().Length.ShouldBe(2);
    }

    [Fact]
    public void IReadOnlyRepository_TEntity_MustHaveClassAndIEntityConstraints()
    {
        var typeParam = _readOnlyRepoType.GetGenericArguments()[0];
        var constraints = typeParam.GetGenericParameterConstraints();

        // Should have class constraint and IEntity<TId> constraint
        (typeParam.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint)
            .ShouldBe(GenericParameterAttributes.ReferenceTypeConstraint);
    }

    [Fact]
    public void IReadOnlyRepository_MustHaveGetByIdAsyncMethod()
    {
        var method = _readOnlyRepoType.GetMethod("GetByIdAsync");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void IReadOnlyRepository_MustHaveGetAllAsyncMethod()
    {
        var method = _readOnlyRepoType.GetMethod("GetAllAsync");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void IReadOnlyRepository_MustHaveFindAsyncMethods()
    {
        var methods = _readOnlyRepoType.GetMethods().Where(m => m.Name == "FindAsync").ToList();
        methods.Count.ShouldBeGreaterThanOrEqualTo(2); // With Specification and Expression
    }

    [Fact]
    public void IReadOnlyRepository_MustHaveFindOneAsyncMethod()
    {
        var method = _readOnlyRepoType.GetMethod("FindOneAsync");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void IReadOnlyRepository_MustHaveAnyAsyncMethods()
    {
        var methods = _readOnlyRepoType.GetMethods().Where(m => m.Name == "AnyAsync").ToList();
        methods.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void IReadOnlyRepository_MustHaveCountAsyncMethods()
    {
        var methods = _readOnlyRepoType.GetMethods().Where(m => m.Name == "CountAsync").ToList();
        methods.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void IReadOnlyRepository_MustHaveGetPagedAsyncMethods()
    {
        var methods = _readOnlyRepoType.GetMethods().Where(m => m.Name == "GetPagedAsync").ToList();
        methods.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    // === IRepository<TEntity, TId> Contract ===

    [Fact]
    public void IRepository_MustBeInterface()
    {
        _repoType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void IRepository_MustExtendIReadOnlyRepository()
    {
        var interfaces = _repoType.GetInterfaces();
        interfaces.ShouldContain(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == _readOnlyRepoType);
    }

    [Fact]
    public void IRepository_MustHaveAddAsyncMethod()
    {
        var method = _repoType.GetMethod("AddAsync");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void IRepository_MustHaveAddRangeAsyncMethod()
    {
        var method = _repoType.GetMethod("AddRangeAsync");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void IRepository_MustHaveUpdateMethod()
    {
        var method = _repoType.GetMethod("Update");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void IRepository_MustHaveUpdateRangeMethod()
    {
        var method = _repoType.GetMethod("UpdateRange");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void IRepository_MustHaveRemoveMethod()
    {
        var method = _repoType.GetMethod("Remove");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void IRepository_MustHaveRemoveRangeMethod()
    {
        var method = _repoType.GetMethod("RemoveRange");
        method.ShouldNotBeNull();
    }

    [Fact]
    public void IRepository_MustHaveRemoveByIdAsyncMethod()
    {
        var method = _repoType.GetMethod("RemoveByIdAsync");
        method.ShouldNotBeNull();
    }

    // === IAggregateRepository<TAggregate, TId> Contract ===

    [Fact]
    public void IAggregateRepository_MustBeInterface()
    {
        _aggregateRepoType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void IAggregateRepository_MustExtendIRepository()
    {
        var interfaces = _aggregateRepoType.GetInterfaces();
        interfaces.ShouldContain(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == _repoType);
    }

    [Fact]
    public void IAggregateRepository_MustHaveSaveAsyncMethod()
    {
        var method = _aggregateRepoType.GetMethod("SaveAsync");
        method.ShouldNotBeNull();
    }

    // === PagedResult<T> Contract ===

    [Fact]
    public void PagedResult_MustBeRecord()
    {
        // Records have a special <Clone>$ method
        _pagedResultType.GetMethod("<Clone>$").ShouldNotBeNull();
    }

    [Fact]
    public void PagedResult_MustHaveItemsProperty()
    {
        var property = _pagedResultType.GetProperty("Items");
        property.ShouldNotBeNull();
        property!.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void PagedResult_MustHavePageNumberProperty()
    {
        var property = _pagedResultType.GetProperty("PageNumber");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(int));
    }

    [Fact]
    public void PagedResult_MustHavePageSizeProperty()
    {
        var property = _pagedResultType.GetProperty("PageSize");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(int));
    }

    [Fact]
    public void PagedResult_MustHaveTotalCountProperty()
    {
        var property = _pagedResultType.GetProperty("TotalCount");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(int));
    }

    [Fact]
    public void PagedResult_MustHaveTotalPagesProperty()
    {
        var property = _pagedResultType.GetProperty("TotalPages");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(int));
    }

    [Fact]
    public void PagedResult_MustHaveHasPreviousPageProperty()
    {
        var property = _pagedResultType.GetProperty("HasPreviousPage");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(bool));
    }

    [Fact]
    public void PagedResult_MustHaveHasNextPageProperty()
    {
        var property = _pagedResultType.GetProperty("HasNextPage");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(bool));
    }

    [Fact]
    public void PagedResult_MustHaveIsEmptyProperty()
    {
        var property = _pagedResultType.GetProperty("IsEmpty");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(bool));
    }

    [Fact]
    public void PagedResult_MustHaveEmptyStaticMethod()
    {
        var method = _pagedResultType.GetMethod("Empty", BindingFlags.Static | BindingFlags.Public);
        method.ShouldNotBeNull();
    }

    [Fact]
    public void PagedResult_MustHaveMapMethod()
    {
        var method = _pagedResultType.GetMethod("Map");
        method.ShouldNotBeNull();
    }

    // === RepositoryError Contract ===

    [Fact]
    public void RepositoryError_MustBeRecord()
    {
        _repoErrorType.GetMethod("<Clone>$").ShouldNotBeNull();
    }

    [Fact]
    public void RepositoryError_MustHaveMessageProperty()
    {
        var property = _repoErrorType.GetProperty("Message");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    public void RepositoryError_MustHaveErrorCodeProperty()
    {
        var property = _repoErrorType.GetProperty("ErrorCode");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    public void RepositoryError_MustHaveEntityTypeProperty()
    {
        var property = _repoErrorType.GetProperty("EntityType");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(Type));
    }

    [Fact]
    public void RepositoryError_MustHaveFactoryMethods()
    {
        var methods = _repoErrorType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .ToList();

        methods.ShouldContain("NotFound");
        methods.ShouldContain("AlreadyExists");
        methods.ShouldContain("ConcurrencyConflict");
        methods.ShouldContain("OperationFailed");
        methods.ShouldContain("InvalidPagination");
    }

    // === RepositoryExtensions Contract ===

    [Fact]
    public void RepositoryExtensions_MustBeStaticClass()
    {
        _repoExtensionsType.IsAbstract.ShouldBeTrue();
        _repoExtensionsType.IsSealed.ShouldBeTrue();
    }

    [Fact]
    public void RepositoryExtensions_MustHaveGetByIdOrErrorAsyncMethod()
    {
        var method = _repoExtensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "GetByIdOrErrorAsync");

        method.ShouldNotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .ShouldBeTrue();
    }

    [Fact]
    public void RepositoryExtensions_MustHaveGetByIdOrThrowAsyncMethod()
    {
        var method = _repoExtensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "GetByIdOrThrowAsync");

        method.ShouldNotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .ShouldBeTrue();
    }

    // === EntityNotFoundException Contract ===

    [Fact]
    public void EntityNotFoundException_MustExtendException()
    {
        _entityNotFoundType.IsSubclassOf(typeof(Exception)).ShouldBeTrue();
    }

    [Fact]
    public void EntityNotFoundException_MustHaveEntityTypeProperty()
    {
        var property = _entityNotFoundType.GetProperty("EntityType");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(Type));
    }

    [Fact]
    public void EntityNotFoundException_MustHaveEntityIdProperty()
    {
        var property = _entityNotFoundType.GetProperty("EntityId");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    public void EntityNotFoundException_MustHaveConstructorWithTypeAndId()
    {
        var constructor = _entityNotFoundType.GetConstructor([typeof(Type), typeof(string)]);
        constructor.ShouldNotBeNull();
    }

    [Fact]
    public void EntityNotFoundException_MustHaveConstructorWithInnerException()
    {
        var constructor = _entityNotFoundType.GetConstructor([typeof(Type), typeof(string), typeof(Exception)]);
        constructor.ShouldNotBeNull();
    }
}
