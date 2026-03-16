#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.ProcessorAgreements.Events;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.Marten.Projections;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="ProcessorProjection"/>.
/// </summary>
public class ProcessorProjectionTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private readonly ProcessorProjection _sut = new();
    private readonly ProjectionContext _context = new();

    #region ProjectionName

    [Fact]
    public void ProjectionName_ReturnsExpectedValue()
    {
        // Arrange & Act
        var name = _sut.ProjectionName;

        // Assert
        name.Should().Be("ProcessorProjection");
    }

    #endregion

    #region Create (ProcessorRegistered)

    [Fact]
    public void Create_FromProcessorRegistered_SetsAllProperties()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var registered = new ProcessorRegistered(
            ProcessorId: processorId,
            Name: "Stripe",
            Country: "US",
            ContactEmail: "dpo@stripe.com",
            ParentProcessorId: parentId,
            Depth: 1,
            AuthorizationType: SubProcessorAuthorizationType.Specific,
            OccurredAtUtc: Now,
            TenantId: "tenant-1",
            ModuleId: "module-billing");

        // Act
        var result = _sut.Create(registered, _context);

        // Assert
        result.Id.Should().Be(processorId);
        result.Name.Should().Be("Stripe");
        result.Country.Should().Be("US");
        result.ContactEmail.Should().Be("dpo@stripe.com");
        result.ParentProcessorId.Should().Be(parentId);
        result.Depth.Should().Be(1);
        result.AuthorizationType.Should().Be(SubProcessorAuthorizationType.Specific);
        result.TenantId.Should().Be("tenant-1");
        result.ModuleId.Should().Be("module-billing");
        result.CreatedAtUtc.Should().Be(Now);
        result.LastModifiedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Create_SetsVersionToOne()
    {
        // Arrange
        var registered = CreateProcessorRegistered();

        // Act
        var result = _sut.Create(registered, _context);

        // Assert
        result.Version.Should().Be(1);
    }

    [Fact]
    public void Create_SetsIsRemovedToFalse()
    {
        // Arrange
        var registered = CreateProcessorRegistered();

        // Act
        var result = _sut.Create(registered, _context);

        // Assert
        result.IsRemoved.Should().BeFalse();
    }

    [Fact]
    public void Create_SetsSubProcessorCountToZero()
    {
        // Arrange
        var registered = CreateProcessorRegistered();

        // Act
        var result = _sut.Create(registered, _context);

        // Assert
        result.SubProcessorCount.Should().Be(0);
    }

    #endregion

    #region Apply (ProcessorUpdated)

    [Fact]
    public void Apply_ProcessorUpdated_UpdatesIdentityFields()
    {
        // Arrange
        var current = CreateProcessorReadModel();
        var updated = new ProcessorUpdated(
            ProcessorId: current.Id,
            Name: "Updated Name",
            Country: "FR",
            ContactEmail: "new@example.com",
            AuthorizationType: SubProcessorAuthorizationType.Specific,
            OccurredAtUtc: Now.AddHours(1));

        // Act
        var result = _sut.Apply(updated, current, _context);

        // Assert
        result.Name.Should().Be("Updated Name");
        result.Country.Should().Be("FR");
        result.ContactEmail.Should().Be("new@example.com");
        result.AuthorizationType.Should().Be(SubProcessorAuthorizationType.Specific);
        result.LastModifiedAtUtc.Should().Be(Now.AddHours(1));
    }

    [Fact]
    public void Apply_ProcessorUpdated_IncrementsVersion()
    {
        // Arrange
        var current = CreateProcessorReadModel();
        var initialVersion = current.Version;
        var updated = new ProcessorUpdated(
            ProcessorId: current.Id,
            Name: "Updated",
            Country: "FR",
            ContactEmail: null,
            AuthorizationType: SubProcessorAuthorizationType.General,
            OccurredAtUtc: Now.AddHours(1));

        // Act
        _sut.Apply(updated, current, _context);

        // Assert
        current.Version.Should().Be(initialVersion + 1);
    }

    #endregion

    #region Apply (ProcessorRemoved)

    [Fact]
    public void Apply_ProcessorRemoved_SetsIsRemovedTrue()
    {
        // Arrange
        var current = CreateProcessorReadModel();
        var removed = new ProcessorRemoved(
            ProcessorId: current.Id,
            Reason: "Contract terminated",
            OccurredAtUtc: Now.AddHours(2));

        // Act
        var result = _sut.Apply(removed, current, _context);

        // Assert
        result.IsRemoved.Should().BeTrue();
        result.LastModifiedAtUtc.Should().Be(Now.AddHours(2));
    }

    #endregion

    #region Apply (SubProcessorAdded / SubProcessorRemoved)

    [Fact]
    public void Apply_SubProcessorAdded_IncrementsSubProcessorCount()
    {
        // Arrange
        var current = CreateProcessorReadModel();
        var initialCount = current.SubProcessorCount;
        var added = new SubProcessorAdded(
            ProcessorId: current.Id,
            SubProcessorId: Guid.NewGuid(),
            SubProcessorName: "Sub-1",
            Depth: 1,
            OccurredAtUtc: Now.AddHours(1));

        // Act
        _sut.Apply(added, current, _context);

        // Assert
        current.SubProcessorCount.Should().Be(initialCount + 1);
    }

    [Fact]
    public void Apply_SubProcessorRemoved_DecrementsSubProcessorCount()
    {
        // Arrange
        var current = CreateProcessorReadModel();
        current.SubProcessorCount = 3;
        var removed = new SubProcessorRemoved(
            ProcessorId: current.Id,
            SubProcessorId: Guid.NewGuid(),
            Reason: "No longer needed",
            OccurredAtUtc: Now.AddHours(1));

        // Act
        _sut.Apply(removed, current, _context);

        // Assert
        current.SubProcessorCount.Should().Be(2);
    }

    [Fact]
    public void Apply_MultipleSubProcessorAdds_CountsCorrectly()
    {
        // Arrange
        var current = CreateProcessorReadModel();

        // Act
        for (var i = 0; i < 5; i++)
        {
            _sut.Apply(
                new SubProcessorAdded(
                    ProcessorId: current.Id,
                    SubProcessorId: Guid.NewGuid(),
                    SubProcessorName: $"Sub-{i}",
                    Depth: 1,
                    OccurredAtUtc: Now.AddMinutes(i)),
                current,
                _context);
        }

        // Assert
        current.SubProcessorCount.Should().Be(5);
    }

    #endregion

    #region Helpers

    private static ProcessorRegistered CreateProcessorRegistered() => new(
        ProcessorId: Guid.NewGuid(),
        Name: "Test Processor",
        Country: "DE",
        ContactEmail: "test@example.com",
        ParentProcessorId: null,
        Depth: 0,
        AuthorizationType: SubProcessorAuthorizationType.General,
        OccurredAtUtc: Now,
        TenantId: null,
        ModuleId: null);

    private static ProcessorReadModel CreateProcessorReadModel() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Processor",
        Country = "DE",
        ContactEmail = "test@example.com",
        ParentProcessorId = null,
        Depth = 0,
        AuthorizationType = SubProcessorAuthorizationType.General,
        IsRemoved = false,
        SubProcessorCount = 0,
        TenantId = null,
        ModuleId = null,
        CreatedAtUtc = Now,
        LastModifiedAtUtc = Now,
        Version = 1
    };

    #endregion
}
