using Encina.Security.Audit;
using FluentAssertions;

namespace Encina.GuardTests.Security.Audit;

/// <summary>
/// Guard clause tests for <see cref="NullPiiMasker"/>.
/// Verifies pass-through behavior and null argument handling.
/// </summary>
public class NullPiiMaskerGuardTests
{
    private readonly NullPiiMasker _masker = new();

    [Fact]
    public void MaskForAuditGeneric_ValidRequest_ReturnsSameInstance()
    {
        var request = new TestRequest { Name = "Test" };

        var result = _masker.MaskForAudit(request);

        result.Should().BeSameAs(request);
    }

    [Fact]
    public void MaskForAuditObject_ValidRequest_ReturnsSameInstance()
    {
        var request = (object)new TestRequest { Name = "Test" };

        var result = _masker.MaskForAudit(request);

        result.Should().BeSameAs(request);
    }

    [Fact]
    public void MaskForAuditGeneric_StringRequest_ReturnsUnchanged()
    {
        var result = _masker.MaskForAudit("sensitive data");

        result.Should().Be("sensitive data");
    }

    [Fact]
    public void MaskForAuditObject_StringRequest_ReturnsUnchanged()
    {
        var result = _masker.MaskForAudit((object)"sensitive data");

        result.Should().Be("sensitive data");
    }

    [Fact]
    public void MaskForAuditGeneric_IntRequest_ReturnsUnchanged()
    {
        var result = _masker.MaskForAudit(42);

        result.Should().Be(42);
    }

    public class TestRequest
    {
        public string? Name { get; set; }
    }
}
