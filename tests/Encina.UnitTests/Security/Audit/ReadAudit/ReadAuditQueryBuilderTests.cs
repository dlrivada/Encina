using Encina.Security.Audit;
using FluentAssertions;

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
        query.UserId.Should().BeNull();
        query.TenantId.Should().BeNull();
        query.EntityType.Should().BeNull();
        query.EntityId.Should().BeNull();
        query.AccessMethod.Should().BeNull();
        query.Purpose.Should().BeNull();
        query.CorrelationId.Should().BeNull();
        query.FromUtc.Should().BeNull();
        query.ToUtc.Should().BeNull();
        query.PageNumber.Should().Be(1);
        query.PageSize.Should().Be(ReadAuditQuery.DefaultPageSize);
    }

    [Fact]
    public void DefaultPageSize_ShouldBe50()
    {
        ReadAuditQuery.DefaultPageSize.Should().Be(50);
    }

    [Fact]
    public void MaxPageSize_ShouldBe1000()
    {
        ReadAuditQuery.MaxPageSize.Should().Be(1000);
    }

    #endregion

    #region Builder Tests

    [Fact]
    public void Builder_ShouldCreateNewInstance()
    {
        // Act
        var builder = ReadAuditQuery.Builder();

        // Assert
        builder.Should().NotBeNull();
    }

    [Fact]
    public void ForUser_ShouldSetUserId()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .ForUser("user-123")
            .Build();

        // Assert
        query.UserId.Should().Be("user-123");
    }

    [Fact]
    public void ForTenant_ShouldSetTenantId()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .ForTenant("tenant-abc")
            .Build();

        // Assert
        query.TenantId.Should().Be("tenant-abc");
    }

    [Fact]
    public void ForEntityType_ShouldSetEntityType()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .ForEntityType("Patient")
            .Build();

        // Assert
        query.EntityType.Should().Be("Patient");
    }

    [Fact]
    public void ForEntity_ShouldSetBothEntityTypeAndEntityId()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .ForEntity("Patient", "P-123")
            .Build();

        // Assert
        query.EntityType.Should().Be("Patient");
        query.EntityId.Should().Be("P-123");
    }

    [Fact]
    public void WithAccessMethod_ShouldSetAccessMethod()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .WithAccessMethod(ReadAccessMethod.Export)
            .Build();

        // Assert
        query.AccessMethod.Should().Be(ReadAccessMethod.Export);
    }

    [Fact]
    public void WithPurpose_ShouldSetPurpose()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .WithPurpose("Patient care")
            .Build();

        // Assert
        query.Purpose.Should().Be("Patient care");
    }

    [Fact]
    public void WithCorrelationId_ShouldSetCorrelationId()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .WithCorrelationId("corr-xyz")
            .Build();

        // Assert
        query.CorrelationId.Should().Be("corr-xyz");
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
        query.FromUtc.Should().Be(from);
        query.ToUtc.Should().Be(to);
    }

    [Fact]
    public void InDateRange_WithNulls_ShouldSetNulls()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .InDateRange(null, null)
            .Build();

        // Assert
        query.FromUtc.Should().BeNull();
        query.ToUtc.Should().BeNull();
    }

    [Fact]
    public void OnPage_ShouldSetPageNumber()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .OnPage(3)
            .Build();

        // Assert
        query.PageNumber.Should().Be(3);
    }

    [Fact]
    public void WithPageSize_ShouldSetPageSize()
    {
        // Act
        var query = ReadAuditQuery.Builder()
            .WithPageSize(100)
            .Build();

        // Assert
        query.PageSize.Should().Be(100);
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
        query.UserId.Should().Be("user-1");
        query.TenantId.Should().Be("tenant-A");
        query.EntityType.Should().Be("Patient");
        query.AccessMethod.Should().Be(ReadAccessMethod.Repository);
        query.Purpose.Should().Be("Audit review");
        query.CorrelationId.Should().Be("corr-1");
        query.FromUtc.Should().Be(from);
        query.ToUtc.Should().Be(to);
        query.PageNumber.Should().Be(2);
        query.PageSize.Should().Be(25);
    }

    #endregion
}
