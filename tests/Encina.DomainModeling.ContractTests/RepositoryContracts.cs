using System.Reflection;
using FluentAssertions;
using LanguageExt;

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
        _readOnlyRepoType.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IReadOnlyRepository_MustBeGenericWithTwoParameters()
    {
        _readOnlyRepoType.IsGenericTypeDefinition.Should().BeTrue();
        _readOnlyRepoType.GetGenericArguments().Should().HaveCount(2);
    }

    [Fact]
    public void IReadOnlyRepository_TEntity_MustHaveClassAndIEntityConstraints()
    {
        var typeParam = _readOnlyRepoType.GetGenericArguments()[0];
        var constraints = typeParam.GetGenericParameterConstraints();

        // Should have class constraint and IEntity<TId> constraint
        (typeParam.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint)
            .Should().Be(GenericParameterAttributes.ReferenceTypeConstraint);
    }

    [Fact]
    public void IReadOnlyRepository_MustHaveGetByIdAsyncMethod()
    {
        var method = _readOnlyRepoType.GetMethod("GetByIdAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void IReadOnlyRepository_MustHaveGetAllAsyncMethod()
    {
        var method = _readOnlyRepoType.GetMethod("GetAllAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void IReadOnlyRepository_MustHaveFindAsyncMethods()
    {
        var methods = _readOnlyRepoType.GetMethods().Where(m => m.Name == "FindAsync").ToList();
        methods.Should().HaveCountGreaterThanOrEqualTo(2); // With Specification and Expression
    }

    [Fact]
    public void IReadOnlyRepository_MustHaveFindOneAsyncMethod()
    {
        var method = _readOnlyRepoType.GetMethod("FindOneAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void IReadOnlyRepository_MustHaveAnyAsyncMethods()
    {
        var methods = _readOnlyRepoType.GetMethods().Where(m => m.Name == "AnyAsync").ToList();
        methods.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void IReadOnlyRepository_MustHaveCountAsyncMethods()
    {
        var methods = _readOnlyRepoType.GetMethods().Where(m => m.Name == "CountAsync").ToList();
        methods.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void IReadOnlyRepository_MustHaveGetPagedAsyncMethods()
    {
        var methods = _readOnlyRepoType.GetMethods().Where(m => m.Name == "GetPagedAsync").ToList();
        methods.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // === IRepository<TEntity, TId> Contract ===

    [Fact]
    public void IRepository_MustBeInterface()
    {
        _repoType.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IRepository_MustExtendIReadOnlyRepository()
    {
        var interfaces = _repoType.GetInterfaces();
        interfaces.Should().Contain(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == _readOnlyRepoType);
    }

    [Fact]
    public void IRepository_MustHaveAddAsyncMethod()
    {
        var method = _repoType.GetMethod("AddAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void IRepository_MustHaveAddRangeAsyncMethod()
    {
        var method = _repoType.GetMethod("AddRangeAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void IRepository_MustHaveUpdateMethod()
    {
        var method = _repoType.GetMethod("Update");
        method.Should().NotBeNull();
    }

    [Fact]
    public void IRepository_MustHaveUpdateRangeMethod()
    {
        var method = _repoType.GetMethod("UpdateRange");
        method.Should().NotBeNull();
    }

    [Fact]
    public void IRepository_MustHaveRemoveMethod()
    {
        var method = _repoType.GetMethod("Remove");
        method.Should().NotBeNull();
    }

    [Fact]
    public void IRepository_MustHaveRemoveRangeMethod()
    {
        var method = _repoType.GetMethod("RemoveRange");
        method.Should().NotBeNull();
    }

    [Fact]
    public void IRepository_MustHaveRemoveByIdAsyncMethod()
    {
        var method = _repoType.GetMethod("RemoveByIdAsync");
        method.Should().NotBeNull();
    }

    // === IAggregateRepository<TAggregate, TId> Contract ===

    [Fact]
    public void IAggregateRepository_MustBeInterface()
    {
        _aggregateRepoType.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IAggregateRepository_MustExtendIRepository()
    {
        var interfaces = _aggregateRepoType.GetInterfaces();
        interfaces.Should().Contain(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == _repoType);
    }

    [Fact]
    public void IAggregateRepository_MustHaveSaveAsyncMethod()
    {
        var method = _aggregateRepoType.GetMethod("SaveAsync");
        method.Should().NotBeNull();
    }

    // === PagedResult<T> Contract ===

    [Fact]
    public void PagedResult_MustBeRecord()
    {
        // Records have a special <Clone>$ method
        _pagedResultType.GetMethod("<Clone>$").Should().NotBeNull();
    }

    [Fact]
    public void PagedResult_MustHaveItemsProperty()
    {
        var property = _pagedResultType.GetProperty("Items");
        property.Should().NotBeNull();
        property!.CanRead.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_MustHavePageNumberProperty()
    {
        var property = _pagedResultType.GetProperty("PageNumber");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<int>();
    }

    [Fact]
    public void PagedResult_MustHavePageSizeProperty()
    {
        var property = _pagedResultType.GetProperty("PageSize");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<int>();
    }

    [Fact]
    public void PagedResult_MustHaveTotalCountProperty()
    {
        var property = _pagedResultType.GetProperty("TotalCount");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<int>();
    }

    [Fact]
    public void PagedResult_MustHaveTotalPagesProperty()
    {
        var property = _pagedResultType.GetProperty("TotalPages");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<int>();
    }

    [Fact]
    public void PagedResult_MustHaveHasPreviousPageProperty()
    {
        var property = _pagedResultType.GetProperty("HasPreviousPage");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<bool>();
    }

    [Fact]
    public void PagedResult_MustHaveHasNextPageProperty()
    {
        var property = _pagedResultType.GetProperty("HasNextPage");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<bool>();
    }

    [Fact]
    public void PagedResult_MustHaveIsEmptyProperty()
    {
        var property = _pagedResultType.GetProperty("IsEmpty");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<bool>();
    }

    [Fact]
    public void PagedResult_MustHaveEmptyStaticMethod()
    {
        var method = _pagedResultType.GetMethod("Empty", BindingFlags.Static | BindingFlags.Public);
        method.Should().NotBeNull();
    }

    [Fact]
    public void PagedResult_MustHaveMapMethod()
    {
        var method = _pagedResultType.GetMethod("Map");
        method.Should().NotBeNull();
    }

    // === RepositoryError Contract ===

    [Fact]
    public void RepositoryError_MustBeRecord()
    {
        _repoErrorType.GetMethod("<Clone>$").Should().NotBeNull();
    }

    [Fact]
    public void RepositoryError_MustHaveMessageProperty()
    {
        var property = _repoErrorType.GetProperty("Message");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
    }

    [Fact]
    public void RepositoryError_MustHaveErrorCodeProperty()
    {
        var property = _repoErrorType.GetProperty("ErrorCode");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
    }

    [Fact]
    public void RepositoryError_MustHaveEntityTypeProperty()
    {
        var property = _repoErrorType.GetProperty("EntityType");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<Type>();
    }

    [Fact]
    public void RepositoryError_MustHaveFactoryMethods()
    {
        var methods = _repoErrorType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .ToList();

        methods.Should().Contain("NotFound");
        methods.Should().Contain("AlreadyExists");
        methods.Should().Contain("ConcurrencyConflict");
        methods.Should().Contain("OperationFailed");
        methods.Should().Contain("InvalidPagination");
    }

    // === RepositoryExtensions Contract ===

    [Fact]
    public void RepositoryExtensions_MustBeStaticClass()
    {
        _repoExtensionsType.IsAbstract.Should().BeTrue();
        _repoExtensionsType.IsSealed.Should().BeTrue();
    }

    [Fact]
    public void RepositoryExtensions_MustHaveGetByIdOrErrorAsyncMethod()
    {
        var method = _repoExtensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "GetByIdOrErrorAsync");

        method.Should().NotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .Should().BeTrue();
    }

    [Fact]
    public void RepositoryExtensions_MustHaveGetByIdOrThrowAsyncMethod()
    {
        var method = _repoExtensionsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "GetByIdOrThrowAsync");

        method.Should().NotBeNull();
        method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .Should().BeTrue();
    }

    // === EntityNotFoundException Contract ===

    [Fact]
    public void EntityNotFoundException_MustExtendException()
    {
        _entityNotFoundType.Should().BeDerivedFrom<Exception>();
    }

    [Fact]
    public void EntityNotFoundException_MustHaveEntityTypeProperty()
    {
        var property = _entityNotFoundType.GetProperty("EntityType");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<Type>();
    }

    [Fact]
    public void EntityNotFoundException_MustHaveEntityIdProperty()
    {
        var property = _entityNotFoundType.GetProperty("EntityId");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
    }

    [Fact]
    public void EntityNotFoundException_MustHaveConstructorWithTypeAndId()
    {
        var constructor = _entityNotFoundType.GetConstructor([typeof(Type), typeof(string)]);
        constructor.Should().NotBeNull();
    }

    [Fact]
    public void EntityNotFoundException_MustHaveConstructorWithInnerException()
    {
        var constructor = _entityNotFoundType.GetConstructor([typeof(Type), typeof(string), typeof(Exception)]);
        constructor.Should().NotBeNull();
    }
}
