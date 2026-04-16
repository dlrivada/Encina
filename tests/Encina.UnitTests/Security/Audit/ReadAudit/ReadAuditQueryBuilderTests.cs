using Encina.Security.Audit;
using Shouldly;

namespace Encina.UnitTests.Security.Audit.ReadAudit;

/// <summary>
/// Unit tests for <see cref="ReadAuditQueryBuilder"/> and <see cref="ReadAuditQuery"/>.
/// </summary>
public class ReadAuditQueryBuilderTests
{
    #region Default Values Tests

    [Fact]
    public void DefaultQuery_ShouldHaveExpectedDefaults()
    {
        // Arrange & Act
        var query = new ReadAuditQuery();

        // Assert
        query.UserId.ShouldBeNull();
        query.TenantId.ShouldBeNull();
        query.EntityType.ShouldBeNull();
        query.EntityId.ShouldBeNull();
        query.AccessMethod.ShouldBeNull();
        query.Purpose.ShouldBeNull();
        query.CorrelationId.ShouldBeNull();
        query.FromUtc.ShouldBeNull();
        query.ToUtc.ShouldBeNull();
        query.PageNumber.ShouldBe(1);
        query.PageSize.ShouldBe(ReadAuditQuery.DefaultPageSize);
    }

    [Fact]
    public void DefaultPageSize_ShouldBe50()
    {
        ReadAuditQuery.DefaultPageSize.ShouldBe(50);
    }

    [Fact]
    public void MaxPageSize_ShouldBe1000()
    {
        ReadAuditQuery.MaxPageSize.ShouldBe(1000);
    }

    #endregion

    #region Builder Tests

    [Fact]
    public void Builder_ShouldCreateNewInstance()
    {
        // Act
        var builder = ReadAuditQuery.Builder();

        // Assert
        builder.ShouldNotBeNull();
    }

    [Fact]
    public void ForUser_ShouldSetUserId()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .ForUser("user-123")
            .Build();

        // Assert
        query.UserId.ShouldBe("user-123");
    }

    [Fact]
    public void ForTenant_ShouldSetTenantId()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .ForTenant("tenant-abc")
            .Build();

        // Assert
        query.TenantId.ShouldBe("tenant-abc");
    }

    [Fact]
    public void ForEntityType_ShouldSetEntityType()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .ForEntityType("Patient")
            .Build();

        // Assert
        query.EntityType.ShouldBe("Patient");
    }

    [Fact]
    public void ForEntity_ShouldSetBothEntityTypeAndEntityId()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .ForEntity("Patient", "P-123")
            .Build();

        // Assert
        query.EntityType.ShouldBe("Patient");
        query.EntityId.ShouldBe("P-123");
    }

    [Fact]
    public void WithAccessMethod_ShouldSetAccessMethod()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .WithAccessMethod(ReadAccessMethod.Export)
            .Build();

        // Assert
        query.AccessMethod.ShouldBe(ReadAccessMethod.Export);
    }

    [Fact]
    public void WithPurpose_ShouldSetPurpose()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .WithPurpose("Patient care")
            .Build();

        // Assert
        query.Purpose.ShouldBe("Patient care");
    }

    [Fact]
    public void WithCorrelationId_ShouldSetCorrelationId()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .WithCorrelationId("corr-xyz")
            .Build();

        // Assert
        query.CorrelationId.ShouldBe("corr-xyz");
    }

    [Fact]
    public void InDateRange_ShouldSetBothDates()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;

        // Act
        var query = ReadAuditQuery.Builder()
            .InDateRange(from, to)
            .Build();

        // Assert
        query.FromUtc.ShouldBe(from);
        query.ToUtc.ShouldBe(to);
    }

    [Fact]
    public void InDateRange_WithNulls_ShouldSetNulls()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .InDateRange(null, null)
            .Build();

        // Assert
        query.FromUtc.ShouldBeNull();
        query.ToUtc.ShouldBeNull();
    }

    [Fact]
    public void OnPage_ShouldSetPageNumber()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .OnPage(3)
            .Build();

        // Assert
        query.PageNumber.ShouldBe(3);
    }

    [Fact]
    public void WithPageSize_ShouldSetPageSize()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .WithPageSize(100)
            .Build();

        // Assert
        query.PageSize.ShouldBe(100);
    }

    [Fact]
    public void Builder_ShouldSupportFullFluentChaining()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;

        // Act
        var query = ReadAuditQuery.Builder()
            .ForUser("user-1")
            .ForTenant("tenant-A")
            .ForEntityType("Patient")
            .WithAccessMethod(ReadAccessMethod.Repository)
            .WithPurpose("Audit review")
            .WithCorrelationId("corr-1")
            .InDateRange(from, to)
            .OnPage(2)
            .WithPageSize(25)
            .Build();

        // Assert
        query.UserId.ShouldBe("user-1");
        query.TenantId.ShouldBe("tenant-A");
        query.EntityType.ShouldBe("Patient");
        query.AccessMethod.ShouldBe(ReadAccessMethod.Repository);
        query.Purpose.ShouldBe("Audit review");
        query.CorrelationId.ShouldBe("corr-1");
        query.FromUtc.ShouldBe(from);
        query.ToUtc.ShouldBe(to);
        query.PageNumber.ShouldBe(2);
        query.PageSize.ShouldBe(25);
    }

    #endregion
}
