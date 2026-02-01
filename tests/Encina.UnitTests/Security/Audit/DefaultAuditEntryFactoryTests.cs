using Encina.Security.Audit;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="DefaultAuditEntryFactory"/>.
/// </summary>
public class DefaultAuditEntryFactoryTests
{
    private readonly IPiiMasker _piiMasker;
    private readonly IOptions<AuditOptions> _options;
    private readonly DefaultAuditEntryFactory _factory;

    public DefaultAuditEntryFactoryTests()
    {
        _piiMasker = Substitute.For<IPiiMasker>();
        _piiMasker.MaskForAudit(Arg.Any<object>()).Returns(x => x.Arg<object>());

        _options = Options.Create(new AuditOptions());
        _factory = new DefaultAuditEntryFactory(_piiMasker, _options);
    }

    [Fact]
    public void Create_ShouldPopulateAllFieldsFromContext()
    {
        // Arrange
        var request = new CreateOrderCommand { Id = Guid.NewGuid() };
        var context = RequestContext.CreateForTest(
            userId: "user-123",
            tenantId: "tenant-456",
            correlationId: "correlation-789");

        // Act
        var entry = _factory.Create(request, context, AuditOutcome.Success, null);

        // Assert
        entry.Id.Should().NotBe(Guid.Empty);
        entry.CorrelationId.Should().Be("correlation-789");
        entry.UserId.Should().Be("user-123");
        entry.TenantId.Should().Be("tenant-456");
        entry.Action.Should().Be("Create");
        entry.EntityType.Should().Be("Order");
        entry.EntityId.Should().Be(request.Id.ToString());
        entry.Outcome.Should().Be(AuditOutcome.Success);
        entry.ErrorMessage.Should().BeNull();
        entry.TimestampUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithErrorMessage_ShouldIncludeIt()
    {
        // Arrange
        var request = new CreateOrderCommand { Id = Guid.NewGuid() };
        var context = RequestContext.CreateForTest();

        // Act
        var entry = _factory.Create(request, context, AuditOutcome.Failure, "Validation failed");

        // Assert
        entry.Outcome.Should().Be(AuditOutcome.Failure);
        entry.ErrorMessage.Should().Be("Validation failed");
    }

    [Fact]
    public void Create_ShouldComputePayloadHash_WhenEnabled()
    {
        // Arrange
        var request = new CreateOrderCommand { Id = Guid.NewGuid() };
        var context = RequestContext.CreateForTest();

        // Act
        var entry = _factory.Create(request, context, AuditOutcome.Success, null);

        // Assert
        entry.RequestPayloadHash.Should().NotBeNullOrEmpty();
        entry.RequestPayloadHash.Should().HaveLength(64); // SHA-256 hex string
    }

    [Fact]
    public void Create_ShouldNotComputePayloadHash_WhenDisabledInOptions()
    {
        // Arrange
        var options = Options.Create(new AuditOptions { IncludePayloadHash = false });
        var factory = new DefaultAuditEntryFactory(_piiMasker, options);
        var request = new CreateOrderCommand { Id = Guid.NewGuid() };
        var context = RequestContext.CreateForTest();

        // Act
        var entry = factory.Create(request, context, AuditOutcome.Success, null);

        // Assert
        entry.RequestPayloadHash.Should().BeNull();
    }

    [Fact]
    public void Create_PayloadHash_ShouldBeDeterministic()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request1 = new CreateOrderCommand { Id = id };
        var request2 = new CreateOrderCommand { Id = id };
        var context = RequestContext.CreateForTest();

        // Act
        var entry1 = _factory.Create(request1, context, AuditOutcome.Success, null);
        var entry2 = _factory.Create(request2, context, AuditOutcome.Success, null);

        // Assert
        entry1.RequestPayloadHash.Should().Be(entry2.RequestPayloadHash);
    }

    [Fact]
    public void Create_PayloadHash_ShouldDifferForDifferentInputs()
    {
        // Arrange
        var request1 = new CreateOrderCommand { Id = Guid.NewGuid() };
        var request2 = new CreateOrderCommand { Id = Guid.NewGuid() };
        var context = RequestContext.CreateForTest();

        // Act
        var entry1 = _factory.Create(request1, context, AuditOutcome.Success, null);
        var entry2 = _factory.Create(request2, context, AuditOutcome.Success, null);

        // Assert
        entry1.RequestPayloadHash.Should().NotBe(entry2.RequestPayloadHash);
    }

    [Fact]
    public void Create_ShouldCallPiiMasker_BeforeHashing()
    {
        // Arrange
        var request = new CreateOrderCommand { Id = Guid.NewGuid() };
        var context = RequestContext.CreateForTest();

        // Act
        _factory.Create(request, context, AuditOutcome.Success, null);

        // Assert
        _piiMasker.Received(1).MaskForAudit(Arg.Any<object>());
    }

    [Fact]
    public void Create_WithAuditableAttribute_ShouldOverrideConventions()
    {
        // Arrange
        var request = new CustomAuditableCommand { Id = Guid.NewGuid() };
        var context = RequestContext.CreateForTest();

        // Act
        var entry = _factory.Create(request, context, AuditOutcome.Success, null);

        // Assert
        entry.EntityType.Should().Be("CustomEntity");
        entry.Action.Should().Be("CustomAction");
    }

    [Fact]
    public void Create_WithAuditableAttribute_IncludePayloadFalse_ShouldNotHash()
    {
        // Arrange
        var request = new NoPayloadCommand { Id = Guid.NewGuid() };
        var context = RequestContext.CreateForTest();

        // Act
        var entry = _factory.Create(request, context, AuditOutcome.Success, null);

        // Assert
        entry.RequestPayloadHash.Should().BeNull();
    }

    [Fact]
    public void Create_WithSensitivityLevel_ShouldIncludeInMetadata()
    {
        // Arrange
        var request = new HighSensitivityCommand { Id = Guid.NewGuid() };
        var context = RequestContext.CreateForTest();

        // Act
        var entry = _factory.Create(request, context, AuditOutcome.Success, null);

        // Assert
        entry.Metadata.Should().ContainKey("SensitivityLevel");
        entry.Metadata["SensitivityLevel"].Should().Be("High");
    }

    [Fact]
    public void Create_ShouldExtractIpAddressFromContext()
    {
        // Arrange
        var request = new CreateOrderCommand { Id = Guid.NewGuid() };
        var context = RequestContext.CreateForTest()
            .WithMetadata("Encina.Audit.IpAddress", "192.168.1.100");

        // Act
        var entry = _factory.Create(request, context, AuditOutcome.Success, null);

        // Assert
        entry.IpAddress.Should().Be("192.168.1.100");
    }

    [Fact]
    public void Create_ShouldExtractUserAgentFromContext()
    {
        // Arrange
        var request = new CreateOrderCommand { Id = Guid.NewGuid() };
        var context = RequestContext.CreateForTest()
            .WithMetadata("Encina.Audit.UserAgent", "Mozilla/5.0");

        // Act
        var entry = _factory.Create(request, context, AuditOutcome.Success, null);

        // Assert
        entry.UserAgent.Should().Be("Mozilla/5.0");
    }

    [Fact]
    public void Create_ShouldCopyNonInternalMetadataFromContext()
    {
        // Arrange
        var request = new CreateOrderCommand { Id = Guid.NewGuid() };
        var context = RequestContext.CreateForTest()
            .WithMetadata("CustomKey", "CustomValue")
            .WithMetadata("Encina.Audit.Internal", "should-be-skipped");

        // Act
        var entry = _factory.Create(request, context, AuditOutcome.Success, null);

        // Assert
        entry.Metadata.Should().ContainKey("CustomKey");
        entry.Metadata["CustomKey"].Should().Be("CustomValue");
        // Internal audit keys are skipped
        entry.Metadata.Should().NotContainKey("Encina.Audit.Internal");
    }

    [Fact]
    public void Create_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var context = RequestContext.CreateForTest();

        // Act
        var act = () => _factory.Create<CreateOrderCommand>(null!, context, AuditOutcome.Success, null);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public void Create_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var request = new CreateOrderCommand { Id = Guid.NewGuid() };

        // Act
        var act = () => _factory.Create(request, null!, AuditOutcome.Success, null);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullPiiMasker_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new DefaultAuditEntryFactory(null!, _options);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("piiMasker");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new DefaultAuditEntryFactory(_piiMasker, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    #region Test Command Types

    private sealed class CreateOrderCommand { public Guid Id { get; init; } }

    [Auditable(EntityType = "CustomEntity", Action = "CustomAction")]
    private sealed class CustomAuditableCommand { public Guid Id { get; init; } }

    [Auditable(IncludePayloadValue = false)]
    private sealed class NoPayloadCommand { public Guid Id { get; init; } }

    [Auditable(SensitivityLevel = "High")]
    private sealed class HighSensitivityCommand { public Guid Id { get; init; } }

    #endregion
}
