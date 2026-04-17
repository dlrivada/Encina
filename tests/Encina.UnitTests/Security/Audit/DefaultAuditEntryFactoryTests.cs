using Encina.Security.Audit;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

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
        entry.Id.ShouldNotBe(Guid.Empty);
        entry.CorrelationId.ShouldBe("correlation-789");
        entry.UserId.ShouldBe("user-123");
        entry.TenantId.ShouldBe("tenant-456");
        entry.Action.ShouldBe("Create");
        entry.EntityType.ShouldBe("Order");
        entry.EntityId.ShouldBe(request.Id.ToString());
        entry.Outcome.ShouldBe(AuditOutcome.Success);
        entry.ErrorMessage.ShouldBeNull();
        entry.TimestampUtc.ShouldBeInRange(DateTime.UtcNow - TimeSpan.FromSeconds(5), DateTime.UtcNow + TimeSpan.FromSeconds(5));
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
        entry.Outcome.ShouldBe(AuditOutcome.Failure);
        entry.ErrorMessage.ShouldBe("Validation failed");
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
        entry.RequestPayloadHash.ShouldNotBeNullOrEmpty();
        entry.RequestPayloadHash.Length.ShouldBe(64); // SHA-256 hex string
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
        entry.RequestPayloadHash.ShouldBeNull();
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
        entry1.RequestPayloadHash.ShouldBe(entry2.RequestPayloadHash);
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
        entry1.RequestPayloadHash.ShouldNotBe(entry2.RequestPayloadHash);
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
        entry.EntityType.ShouldBe("CustomEntity");
        entry.Action.ShouldBe("CustomAction");
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
        entry.RequestPayloadHash.ShouldBeNull();
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
        entry.Metadata.ShouldContainKey("SensitivityLevel");
        entry.Metadata["SensitivityLevel"].ShouldBe("High");
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
        entry.IpAddress.ShouldBe("192.168.1.100");
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
        entry.UserAgent.ShouldBe("Mozilla/5.0");
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
        entry.Metadata.ShouldContainKey("CustomKey");
        entry.Metadata["CustomKey"].ShouldBe("CustomValue");
        // Internal audit keys are skipped
        entry.Metadata.ShouldNotContainKey("Encina.Audit.Internal");
    }

    [Fact]
    public void Create_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var context = RequestContext.CreateForTest();

        // Act
        var act = () => _factory.Create<CreateOrderCommand>(null!, context, AuditOutcome.Success, null);

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("request");
    }

    [Fact]
    public void Create_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var request = new CreateOrderCommand { Id = Guid.NewGuid() };

        // Act
        var act = () => _factory.Create(request, null!, AuditOutcome.Success, null);

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("context");
    }

    [Fact]
    public void Constructor_WithNullPiiMasker_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new DefaultAuditEntryFactory(null!, _options);

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("piiMasker");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new DefaultAuditEntryFactory(_piiMasker, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("options");
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
