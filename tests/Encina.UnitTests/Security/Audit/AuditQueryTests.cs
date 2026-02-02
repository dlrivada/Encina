using Encina.Security.Audit;
using FluentAssertions;

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
        query.PageNumber.Should().Be(1);
    }

    [Fact]
    public void DefaultValues_PageSize_ShouldBeDefaultPageSize()
    {
        // Act
        var query = new AuditQuery();

        // Assert
        query.PageSize.Should().Be(AuditQuery.DefaultPageSize);
        query.PageSize.Should().Be(50);
    }

    [Fact]
    public void DefaultValues_AllFilters_ShouldBeNull()
    {
        // Act
        var query = new AuditQuery();

        // Assert
        query.UserId.Should().BeNull();
        query.TenantId.Should().BeNull();
        query.EntityType.Should().BeNull();
        query.EntityId.Should().BeNull();
        query.Action.Should().BeNull();
        query.Outcome.Should().BeNull();
        query.CorrelationId.Should().BeNull();
        query.FromUtc.Should().BeNull();
        query.ToUtc.Should().BeNull();
        query.IpAddress.Should().BeNull();
        query.MinDuration.Should().BeNull();
        query.MaxDuration.Should().BeNull();
    }

    [Fact]
    public void Constants_DefaultPageSize_ShouldBe50()
    {
        // Assert
        AuditQuery.DefaultPageSize.Should().Be(50);
    }

    [Fact]
    public void Constants_MaxPageSize_ShouldBe1000()
    {
        // Assert
        AuditQuery.MaxPageSize.Should().Be(1000);
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
        query.UserId.Should().Be("user-123");
        query.TenantId.Should().Be("tenant-456");
        query.EntityType.Should().Be("Order");
        query.EntityId.Should().Be("order-789");
        query.Action.Should().Be("Create");
        query.Outcome.Should().Be(AuditOutcome.Success);
        query.CorrelationId.Should().Be("corr-abc");
        query.FromUtc.Should().Be(fromUtc);
        query.ToUtc.Should().Be(toUtc);
        query.IpAddress.Should().Be("192.168.1.1");
        query.MinDuration.Should().Be(TimeSpan.FromMilliseconds(100));
        query.MaxDuration.Should().Be(TimeSpan.FromSeconds(5));
        query.PageNumber.Should().Be(2);
        query.PageSize.Should().Be(25);
    }

    #endregion

    #region Builder Tests

    [Fact]
    public void Builder_ShouldReturnNewBuilderInstance()
    {
        // Act
        var builder = AuditQuery.Builder();

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeOfType<AuditQueryBuilder>();
    }

    [Fact]
    public void Builder_Build_WithNoFilters_ShouldReturnDefaultQuery()
    {
        // Act
        var query = AuditQuery.Builder().Build();

        // Assert
        query.PageNumber.Should().Be(1);
        query.PageSize.Should().Be(AuditQuery.DefaultPageSize);
        query.UserId.Should().BeNull();
    }

    [Fact]
    public void Builder_ForUser_ShouldSetUserId()
    {
        // Act
        var query = AuditQuery.Builder()
            .ForUser("user-123")
            .Build();

        // Assert
        query.UserId.Should().Be("user-123");
    }

    [Fact]
    public void Builder_ForTenant_ShouldSetTenantId()
    {
        // Act
        var query = AuditQuery.Builder()
            .ForTenant("tenant-456")
            .Build();

        // Assert
        query.TenantId.Should().Be("tenant-456");
    }

    [Fact]
    public void Builder_ForEntityType_ShouldSetEntityType()
    {
        // Act
        var query = AuditQuery.Builder()
            .ForEntityType("Order")
            .Build();

        // Assert
        query.EntityType.Should().Be("Order");
    }

    [Fact]
    public void Builder_ForEntity_ShouldSetEntityTypeAndId()
    {
        // Act
        var query = AuditQuery.Builder()
            .ForEntity("Order", "order-123")
            .Build();

        // Assert
        query.EntityType.Should().Be("Order");
        query.EntityId.Should().Be("order-123");
    }

    [Fact]
    public void Builder_WithAction_ShouldSetAction()
    {
        // Act
        var query = AuditQuery.Builder()
            .WithAction("Create")
            .Build();

        // Assert
        query.Action.Should().Be("Create");
    }

    [Fact]
    public void Builder_WithOutcome_ShouldSetOutcome()
    {
        // Act
        var query = AuditQuery.Builder()
            .WithOutcome(AuditOutcome.Failure)
            .Build();

        // Assert
        query.Outcome.Should().Be(AuditOutcome.Failure);
    }

    [Fact]
    public void Builder_WithCorrelationId_ShouldSetCorrelationId()
    {
        // Act
        var query = AuditQuery.Builder()
            .WithCorrelationId("corr-123")
            .Build();

        // Assert
        query.CorrelationId.Should().Be("corr-123");
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
        query.FromUtc.Should().Be(from);
        query.ToUtc.Should().Be(to);
    }

    [Fact]
    public void Builder_InDateRange_WithNulls_ShouldSetNulls()
    {
        // Act
        var query = AuditQuery.Builder()
            .InDateRange(null, null)
            .Build();

        // Assert
        query.FromUtc.Should().BeNull();
        query.ToUtc.Should().BeNull();
    }

    [Fact]
    public void Builder_FromIpAddress_ShouldSetIpAddress()
    {
        // Act
        var query = AuditQuery.Builder()
            .FromIpAddress("192.168.1.100")
            .Build();

        // Assert
        query.IpAddress.Should().Be("192.168.1.100");
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
        query.MinDuration.Should().Be(min);
        query.MaxDuration.Should().Be(max);
    }

    [Fact]
    public void Builder_OnPage_ShouldSetPageNumber()
    {
        // Act
        var query = AuditQuery.Builder()
            .OnPage(5)
            .Build();

        // Assert
        query.PageNumber.Should().Be(5);
    }

    [Fact]
    public void Builder_WithPageSize_ShouldSetPageSize()
    {
        // Act
        var query = AuditQuery.Builder()
            .WithPageSize(100)
            .Build();

        // Assert
        query.PageSize.Should().Be(100);
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
        builder.Should().BeOfType<AuditQueryBuilder>();
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
        query.UserId.Should().Be("user-123");
        query.TenantId.Should().Be("tenant-456");
        query.EntityType.Should().Be("Order");
        query.EntityId.Should().Be("order-789");
        query.Action.Should().Be("Update");
        query.Outcome.Should().Be(AuditOutcome.Success);
        query.CorrelationId.Should().Be("corr-abc");
        query.FromUtc.Should().Be(from);
        query.ToUtc.Should().Be(to);
        query.IpAddress.Should().Be("10.0.0.1");
        query.MinDuration.Should().Be(TimeSpan.FromMilliseconds(50));
        query.MaxDuration.Should().Be(TimeSpan.FromSeconds(2));
        query.PageNumber.Should().Be(3);
        query.PageSize.Should().Be(75);
    }

    #endregion
}
