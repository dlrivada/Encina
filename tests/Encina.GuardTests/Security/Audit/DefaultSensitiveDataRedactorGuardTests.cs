using Encina.Security.Audit;
using Shouldly;
using Microsoft.Extensions.Options;

namespace Encina.GuardTests.Security.Audit;

/// <summary>
/// Additional guard clause tests for <see cref="DefaultSensitiveDataRedactor"/>.
/// Tests method overloads with additional sensitive fields parameters.
/// </summary>
public class DefaultSensitiveDataRedactorGuardTests
{
    private static readonly string[] AdditionalFields = ["Name"];
    private readonly DefaultSensitiveDataRedactor _redactor;

    public DefaultSensitiveDataRedactorGuardTests()
    {
        _redactor = new DefaultSensitiveDataRedactor(Options.Create(new AuditOptions()));
    }

    [Fact]
    public void MaskForAuditGenericWithFields_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _redactor.MaskForAudit<TestRequest>(null!, (IEnumerable<string>?)null);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("request");
    }

    [Fact]
    public void MaskForAuditObjectWithFields_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _redactor.MaskForAudit((object)null!, (IEnumerable<string>?)null);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("request");
    }

    [Fact]
    public void MaskForAuditGenericWithFields_ValidRequest_DoesNotThrow()
    {
        var request = new TestRequest { Name = "Test", Password = "secret" };

        var act = () => _redactor.MaskForAudit(request, AdditionalFields);

        Should.NotThrow(act);
    }

    [Fact]
    public void MaskForAuditObjectWithFields_ValidRequest_DoesNotThrow()
    {
        var request = new TestRequest { Name = "Test", Password = "secret" };

        var act = () => _redactor.MaskForAudit((object)request, AdditionalFields);

        Should.NotThrow(act);
    }

    [Fact]
    public void RedactJsonString_NullJson_DoesNotThrow()
    {
        var act = () => _redactor.RedactJsonString(null!);

        Should.NotThrow(act);
    }

    [Fact]
    public void RedactJsonString_EmptyJson_DoesNotThrow()
    {
        var act = () => _redactor.RedactJsonString(string.Empty);

        Should.NotThrow(act);
    }

    [Fact]
    public void RedactJsonString_WhitespaceJson_DoesNotThrow()
    {
        var act = () => _redactor.RedactJsonString("   ");

        Should.NotThrow(act);
    }

    [Fact]
    public void RedactJsonString_ValidJson_DoesNotThrow()
    {
        var act = () => _redactor.RedactJsonString("{\"name\":\"test\",\"password\":\"secret\"}");

        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultSensitiveDataRedactor(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    public class TestRequest
    {
        public string? Name { get; set; }
        public string? Password { get; set; }
    }
}
