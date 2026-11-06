using System.Data;
using Encina.Dapper.SqlServer.Tenancy;
using Encina.DomainModeling;
using Encina.Tenancy;
using Encina.Testing.Shouldly;
using LanguageExt;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.SqlServer.Tenancy;

/// <summary>
/// Unit tests for the missing tenant context behaviour of
/// <see cref="TenantAwareFunctionalRepositoryDapper{TEntity, TId}"/> (#3300): every operation returns
/// <c>Repository.TenantRequired</c> as a <c>Left</c> naming the operation, never throws, and never
/// reaches the database connection.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TenantAwareFunctionalRepositoryDapperTenantRequiredTests
{
    private static readonly string[] AllOperations =
    [
        "GetById", "List", "ListSpec", "FirstOrDefault", "Count", "CountSpec", "Any", "AnySpec",
        "GetPaged", "GetPagedSpec", "Add", "AddRange", "Update", "UpdateNoOwnershipCheck",
        "UpdateRange", "UpdateRangeNoOwnershipCheck", "DeleteById", "DeleteEntity",
        "DeleteEntityNoOwnershipCheck", "DeleteRange"
    ];

    public static TheoryData<string, string?> OperationsAndMissingTenantIds()
    {
        var data = new TheoryData<string, string?>();
        foreach (var tenantId in new string?[] { null, string.Empty })
        {
            foreach (var operation in AllOperations)
            {
                data.Add(operation, tenantId);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(OperationsAndMissingTenantIds))]
    public async Task Operation_WithoutTenantContext_ReturnsTenantRequiredLeft_WithoutTouchingTheConnection(
        string operation,
        string? tenantId)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var validateOnModify = !operation.EndsWith("NoOwnershipCheck", StringComparison.Ordinal);
        var repository = CreateRepository(connection, tenantId, new DapperTenancyOptions
        {
            ThrowOnMissingTenantContext = true,
            ValidateTenantOnModify = validateOnModify
        });
        var order = new TenantTestOrder { Id = Guid.NewGuid(), IsActive = true };

        // Act
        var error = await RunAsync(repository, operation, order);

        // Assert
        error.ShouldNotBeNull();
        error.Value.GetCode().IfNone(string.Empty).ShouldBe(RepositoryErrors.TenantRequiredErrorCode);
        error.Value.GetDetails()["Operation"].ShouldBe(ExpectedOperation(operation));
        error.Value.GetDetails()["EntityType"].ShouldBe(nameof(TenantTestOrder));
        error.Value.Message.ShouldNotContain("tenant-");
        connection.ReceivedCalls().ShouldBeEmpty();
        order.TenantId.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task UpdateAsync_WithoutTenantContext_DoesNotPopulateAuditFields()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var repository = CreateAuditedRepository(connection, null, new DapperTenancyOptions { ValidateTenantOnModify = false });
        var order = new AuditedTenantOrder { Id = Guid.NewGuid() };

        // Act
        var result = await repository.UpdateAsync(order);

        // Assert
        result.ShouldBeError().GetCode().IfNone(string.Empty).ShouldBe(RepositoryErrors.TenantRequiredErrorCode);
        order.ModifiedAtUtc.ShouldBeNull();
        connection.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Operations_WithoutTenantContext_AndOptOut_ReachTheConnection()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var repository = CreateRepository(connection, null, new DapperTenancyOptions { ThrowOnMissingTenantContext = false });

        // Act
        var count = await repository.CountAsync();
        var add = await repository.AddAsync(new TenantTestOrder { Id = Guid.NewGuid() });

        // Assert
        ErrorCodeOf(count).ShouldBe(RepositoryErrors.PersistenceErrorErrorCode);
        ErrorCodeOf(add).ShouldBe(RepositoryErrors.PersistenceErrorErrorCode);
        connection.ReceivedCalls().ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("Update")]
    [InlineData("UpdateRange")]
    [InlineData("Delete")]
    public async Task Modify_WithDifferentTenant_ReturnsInvalidOperationNamingTheOperation(string operation)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var repository = CreateRepository(connection, "tenant-1", new DapperTenancyOptions());
        var order = new TenantTestOrder { Id = Guid.NewGuid(), TenantId = "tenant-2" };

        // Act
        var error = operation switch
        {
            "Update" => ErrorOf(await repository.UpdateAsync(order)),
            "UpdateRange" => ErrorOf(await repository.UpdateRangeAsync([order])),
            _ => ErrorOf(await repository.DeleteAsync(order))
        };

        // Assert
        error.ShouldNotBeNull();
        error.Value.GetCode().IfNone(string.Empty).ShouldBe(RepositoryErrors.InvalidOperationErrorCode);
        error.Value.GetDetails()["Operation"].ShouldBe(operation);
        connection.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateRangeAsync_WithEmptyList_SucceedsWithoutTenantContext()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var repository = CreateRepository(connection, null, new DapperTenancyOptions());

        // Act
        var result = await repository.UpdateRangeAsync([]);

        // Assert
        result.ShouldBeSuccess();
        connection.ReceivedCalls().ShouldBeEmpty();
    }

    private static string ExpectedOperation(string operation) => operation switch
    {
        "ListSpec" => "List",
        "CountSpec" => "Count",
        "AnySpec" => "Any",
        "GetPagedSpec" => "GetPaged",
        "UpdateNoOwnershipCheck" => "Update",
        "UpdateRangeNoOwnershipCheck" => "UpdateRange",
        "DeleteById" or "DeleteEntity" or "DeleteEntityNoOwnershipCheck" => "Delete",
        _ => operation
    };

    private static TenantAwareFunctionalRepositoryDapper<AuditedTenantOrder, Guid> CreateAuditedRepository(
        IDbConnection connection,
        string? tenantId,
        DapperTenancyOptions options)
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns(tenantId);

        var mapping = new TenantEntityMappingBuilder<AuditedTenantOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .Build()
            .ShouldBeSuccess();

        return new TenantAwareFunctionalRepositoryDapper<AuditedTenantOrder, Guid>(connection, mapping, tenantProvider, options);
    }

    private static TenantAwareFunctionalRepositoryDapper<TenantTestOrder, Guid> CreateRepository(
        IDbConnection connection,
        string? tenantId,
        DapperTenancyOptions options)
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns(tenantId);

        var mapping = new TenantEntityMappingBuilder<TenantTestOrder, Guid>()
            .ToTable("Orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapProperty(o => o.CustomerId)
            .MapProperty(o => o.Total)
            .MapProperty(o => o.IsActive)
            .Build()
            .ShouldBeSuccess();

        return new TenantAwareFunctionalRepositoryDapper<TenantTestOrder, Guid>(connection, mapping, tenantProvider, options);
    }

    private static async Task<EncinaError?> RunAsync(
        TenantAwareFunctionalRepositoryDapper<TenantTestOrder, Guid> repository,
        string operation,
        TenantTestOrder order)
    {
        var spec = new TenantActiveOrdersSpec();
        var page = new PaginationOptions(1, 10);

        return operation switch
        {
            "GetById" => ErrorOf(await repository.GetByIdAsync(order.Id)),
            "List" => ErrorOf(await repository.ListAsync()),
            "ListSpec" => ErrorOf(await repository.ListAsync(spec)),
            "FirstOrDefault" => ErrorOf(await repository.FirstOrDefaultAsync(spec)),
            "Count" => ErrorOf(await repository.CountAsync()),
            "CountSpec" => ErrorOf(await repository.CountAsync(spec)),
            "Any" => ErrorOf(await repository.AnyAsync()),
            "AnySpec" => ErrorOf(await repository.AnyAsync(spec)),
            "GetPaged" => ErrorOf(await repository.GetPagedAsync(page)),
            "GetPagedSpec" => ErrorOf(await repository.GetPagedAsync(spec, page)),
            "Add" => ErrorOf(await repository.AddAsync(order)),
            "AddRange" => ErrorOf(await repository.AddRangeAsync([order])),
            "Update" or "UpdateNoOwnershipCheck" => ErrorOf(await repository.UpdateAsync(order)),
            "UpdateRange" or "UpdateRangeNoOwnershipCheck" => ErrorOf(await repository.UpdateRangeAsync([order])),
            "DeleteById" => ErrorOf(await repository.DeleteAsync(order.Id)),
            "DeleteEntity" or "DeleteEntityNoOwnershipCheck" => ErrorOf(await repository.DeleteAsync(order)),
            "DeleteRange" => ErrorOf(await repository.DeleteRangeAsync(spec)),
            _ => throw new InvalidOperationException($"Unknown operation {operation}")
        };
    }

    private static EncinaError? ErrorOf<T>(Either<EncinaError, T> either)
        => either.Match<EncinaError?>(Right: _ => null, Left: error => error);

    private static string ErrorCodeOf<T>(Either<EncinaError, T> either)
        => ErrorOf(either)?.GetCode().IfNone(string.Empty) ?? string.Empty;

    private sealed class AuditedTenantOrder : ITenantEntity, IModifiedAtUtc
    {
        public Guid Id { get; set; }

        public string TenantId { get; set; } = string.Empty;

        public DateTime? ModifiedAtUtc { get; set; }
    }
}
