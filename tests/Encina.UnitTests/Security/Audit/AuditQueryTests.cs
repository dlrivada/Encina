using Encina.Security.Audit;
using Shouldly;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="AuditQuery"/> and <see cref="AuditQueryBuilder"/>.
/// </summary>
public class AuditQueryTests
{
    #region Default Values Tests

    [Fact]
    public void DefaultValues_PageNumber_ShouldBeOne()
    {
        // Act
        var query = new AuditQuery();

        // Assert
        query.PageNumber.ShouldBe(1);
    }

    [Fact]
    public void DefaultValues_PageSize_ShouldBeDefaultPageSize()
    {
        // Act
        var query = new AuditQuery();

        // Assert
        query.PageSize.ShouldBe(AuditQuery.DefaultPageSize);
        query.PageSize.ShouldBe(50);
    }

    [Fact]
    public void DefaultValues_AllFilters_ShouldBeNull()
    {
        // Act
        var query = new AuditQuery();

        // Assert
        query.UserId.ShouldBeNull();
        query.TenantId.ShouldBeNull();
        query.EntityType.ShouldBeNull();
        query.EntityId.ShouldBeNull();
        query.Action.ShouldBeNull();
        query.Outcome.ShouldBeNull();
        query.CorrelationId.ShouldBeNull();
        query.FromUtc.ShouldBeNull();
        query.ToUtc.ShouldBeNull();
        query.IpAddress.ShouldBeNull();
        query.MinDuration.ShouldBeNull();
        query.MaxDuration.ShouldBeNull();
    }

    [Fact]
    public void Constants_DefaultPageSize_ShouldBe50()
    {
        // Assert
        AuditQuery.DefaultPageSize.ShouldBe(50);
    }

    [Fact]
    public void Constants_MaxPageSize_ShouldBe1000()
    {
        // Assert
        AuditQuery.MaxPageSize.ShouldBe(1000);
    }

    #endregion

    #region Property Setting Tests

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var fromUtc = DateTime.UtcNow.AddDays(-7);
        var toUtc = DateTime.UtcNow;

        // Act
        var query = new AuditQuery
        {
            UserId = "user-123",
            TenantId = "tenant-456",
            EntityType = "Order",
            EntityId = "order-789",
            Action = "Create",
            Outcome = AuditOutcome.Success,
            CorrelationId = "corr-abc",
            FromUtc = fromUtc,
            ToUtc = toUtc,
            IpAddress = "192.168.1.1",
            MinDuration = TimeSpan.FromMilliseconds(100),
            MaxDuration = TimeSpan.FromSeconds(5),
            PageNumber = 2,
            PageSize = 25
        };

        // Assert
        query.UserId.ShouldBe("user-123");
        query.TenantId.ShouldBe("tenant-456");
        query.EntityType.ShouldBe("Order");
        query.EntityId.ShouldBe("order-789");
        query.Action.ShouldBe("Create");
        query.Outcome.ShouldBe(AuditOutcome.Success);
        query.CorrelationId.ShouldBe("corr-abc");
        query.FromUtc.ShouldBe(fromUtc);
        query.ToUtc.ShouldBe(toUtc);
        query.IpAddress.ShouldBe("192.168.1.1");
        query.MinDuration.ShouldBe(TimeSpan.FromMilliseconds(100));
        query.MaxDuration.ShouldBe(TimeSpan.FromSeconds(5));
        query.PageNumber.ShouldBe(2);
        query.PageSize.ShouldBe(25);
    }

    #endregion

    #region Builder Tests

    [Fact]
    public void Builder_ShouldReturnNewBuilderInstance()
    {
        // Act
        var builder = AuditQuery.Builder();

        // Assert
        builder.ShouldNotBeNull();
        builder.ShouldBeOfType<AuditQueryBuilder>();
    }

    [Fact]
    public void Builder_Build_WithNoFilters_ShouldReturnDefaultQuery()
    {
        // Act
        var query = AuditQuery.Builder().Build();

        // Assert
        query.PageNumber.ShouldBe(1);
        query.PageSize.ShouldBe(AuditQuery.DefaultPageSize);
        query.UserId.ShouldBeNull();
    }

    [Fact]
    public void Builder_ForUser_ShouldSetUserId()
    {
        // Act
        var query = AuditQuery.Builder()
            .ForUser("user-123")
            .Build();

        // Assert
        query.UserId.ShouldBe("user-123");
    }

    [Fact]
    public void Builder_ForTenant_ShouldSetTenantId()
    {
        // Act
        var query = AuditQuery.Builder()
            .ForTenant("tenant-456")
            .Build();

        // Assert
        query.TenantId.ShouldBe("tenant-456");
    }

    [Fact]
    public void Builder_ForEntityType_ShouldSetEntityType()
    {
        // Act
        var query = AuditQuery.Builder()
            .ForEntityType("Order")
            .Build();

        // Assert
        query.EntityType.ShouldBe("Order");
    }

    [Fact]
    public void Builder_ForEntity_ShouldSetEntityTypeAndId()
    {
        // Act
        var query = AuditQuery.Builder()
            .ForEntity("Order", "order-123")
            .Build();

        // Assert
        query.EntityType.ShouldBe("Order");
        query.EntityId.ShouldBe("order-123");
    }

    [Fact]
    public void Builder_WithAction_ShouldSetAction()
    {
        // Act
        var query = AuditQuery.Builder()
            .WithAction("Create")
            .Build();

        // Assert
        query.Action.ShouldBe("Create");
    }

    [Fact]
    public void Builder_WithOutcome_ShouldSetOutcome()
    {
        // Act
        var query = AuditQuery.Builder()
            .WithOutcome(AuditOutcome.Failure)
            .Build();

        // Assert
        query.Outcome.ShouldBe(AuditOutcome.Failure);
    }

    [Fact]
    public void Builder_WithCorrelationId_ShouldSetCorrelationId()
    {
        // Act
        var query = AuditQuery.Builder()
            .WithCorrelationId("corr-123")
            .Build();

        // Assert
        query.CorrelationId.ShouldBe("corr-123");
    }

    [Fact]
    public void Builder_InDateRange_ShouldSetFromAndToUtc()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        // Act
        var query = AuditQuery.Builder()
            .InDateRange(from, to)
            .Build();

        // Assert
        query.FromUtc.ShouldBe(from);
        query.ToUtc.ShouldBe(to);
    }

    [Fact]
    public void Builder_InDateRange_WithNulls_ShouldSetNulls()
    {
        // Act
        var query = AuditQuery.Builder()
            .InDateRange(null, null)
            .Build();

        // Assert
        query.FromUtc.ShouldBeNull();
        query.ToUtc.ShouldBeNull();
    }

    [Fact]
    public void Builder_FromIpAddress_ShouldSetIpAddress()
    {
        // Act
        var query = AuditQuery.Builder()
            .FromIpAddress("192.168.1.100")
            .Build();

        // Assert
        query.IpAddress.ShouldBe("192.168.1.100");
    }

    [Fact]
    public void Builder_WithDurationRange_ShouldSetMinAndMaxDuration()
    {
        // Arrange
        var min = TimeSpan.FromMilliseconds(100);
        var max = TimeSpan.FromSeconds(10);

        // Act
        var query = AuditQuery.Builder()
            .WithDurationRange(min, max)
            .Build();

        // Assert
        query.MinDuration.ShouldBe(min);
        query.MaxDuration.ShouldBe(max);
    }

    [Fact]
    public void Builder_OnPage_ShouldSetPageNumber()
    {
        // Act
        var query = AuditQuery.Builder()
            .OnPage(5)
            .Build();

        // Assert
        query.PageNumber.ShouldBe(5);
    }

    [Fact]
    public void Builder_WithPageSize_ShouldSetPageSize()
    {
        // Act
        var query = AuditQuery.Builder()
            .WithPageSize(100)
            .Build();

        // Assert
        query.PageSize.ShouldBe(100);
    }

    [Fact]
    public void Builder_FluentChaining_ShouldReturnBuilderInstance()
    {
        // Act
        var builder = AuditQuery.Builder()
            .ForUser("user")
            .ForTenant("tenant")
            .ForEntityType("Order")
            .WithAction("Create")
            .WithOutcome(AuditOutcome.Success)
            .OnPage(2)
            .WithPageSize(25);

        // Assert
        builder.ShouldBeOfType<AuditQueryBuilder>();
    }

    [Fact]
    public void Builder_ComplexQuery_ShouldSetAllProperties()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        // Act
        var query = AuditQuery.Builder()
            .ForUser("user-123")
            .ForTenant("tenant-456")
            .ForEntity("Order", "order-789")
            .WithAction("Update")
            .WithOutcome(AuditOutcome.Success)
            .WithCorrelationId("corr-abc")
            .InDateRange(from, to)
            .FromIpAddress("10.0.0.1")
            .WithDurationRange(TimeSpan.FromMilliseconds(50), TimeSpan.FromSeconds(2))
            .OnPage(3)
            .WithPageSize(75)
            .Build();

        // Assert
        query.UserId.ShouldBe("user-123");
        query.TenantId.ShouldBe("tenant-456");
        query.EntityType.ShouldBe("Order");
        query.EntityId.ShouldBe("order-789");
        query.Action.ShouldBe("Update");
        query.Outcome.ShouldBe(AuditOutcome.Success);
        query.CorrelationId.ShouldBe("corr-abc");
        query.FromUtc.ShouldBe(from);
        query.ToUtc.ShouldBe(to);
        query.IpAddress.ShouldBe("10.0.0.1");
        query.MinDuration.ShouldBe(TimeSpan.FromMilliseconds(50));
        query.MaxDuration.ShouldBe(TimeSpan.FromSeconds(2));
        query.PageNumber.ShouldBe(3);
        query.PageSize.ShouldBe(75);
    }

    #endregion
}
