using Encina.Security.Audit;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="DefaultSensitiveDataRedactor"/>.
/// </summary>
public class DefaultSensitiveDataRedactorTests
{
    private readonly DefaultSensitiveDataRedactor _redactor;

    public DefaultSensitiveDataRedactorTests()
    {
        var options = Options.Create(new AuditOptions());
        _redactor = new DefaultSensitiveDataRedactor(options);
    }

    #region Simple Field Redaction Tests

    [Fact]
    public void MaskForAudit_SimpleFieldWithPassword_ShouldRedact()
    {
        // Arrange
        var request = new SimpleRequest { Email = "user@example.com", Password = "secret123" };

        // Act
        var masked = _redactor.MaskForAudit(request);

        // Assert
        masked.Email.Should().Be("user@example.com");
        masked.Password.Should().Be(DefaultSensitiveDataRedactor.RedactedValue);
    }

    [Fact]
    public void MaskForAudit_SimpleFieldWithToken_ShouldRedact()
    {
        // Arrange
        var request = new TokenRequest { UserId = "user-1", Token = "abc123" };

        // Act
        var masked = _redactor.MaskForAudit(request);

        // Assert
        masked.UserId.Should().Be("user-1");
        masked.Token.Should().Be(DefaultSensitiveDataRedactor.RedactedValue);
    }

    [Fact]
    public void MaskForAudit_SimpleFieldWithApiKey_ShouldRedact()
    {
        // Arrange
        var request = new ApiKeyRequest { Name = "Test", ApiKey = "key-12345" };

        // Act
        var masked = _redactor.MaskForAudit(request);

        // Assert
        masked.Name.Should().Be("Test");
        masked.ApiKey.Should().Be(DefaultSensitiveDataRedactor.RedactedValue);
    }

    [Fact]
    public void MaskForAudit_MultipleSensitiveFields_ShouldRedactAll()
    {
        // Arrange
        var request = new MultiSensitiveRequest
        {
            Username = "testuser",
            Password = "secret",
            Token = "abc",
            ApiKey = "key123",
            Ssn = "123-45-6789"
        };

        // Act
        var masked = _redactor.MaskForAudit(request);

        // Assert
        masked.Username.Should().Be("testuser");
        masked.Password.Should().Be(DefaultSensitiveDataRedactor.RedactedValue);
        masked.Token.Should().Be(DefaultSensitiveDataRedactor.RedactedValue);
        masked.ApiKey.Should().Be(DefaultSensitiveDataRedactor.RedactedValue);
        masked.Ssn.Should().Be(DefaultSensitiveDataRedactor.RedactedValue);
    }

    #endregion

    #region Nested Object Redaction Tests

    [Fact]
    public void MaskForAudit_NestedObjectWithSensitiveField_ShouldRedactViaJsonString()
    {
        // Test using RedactJsonString which is what MaskForAudit uses internally
        // This verifies the JSON-level redaction works correctly
        // Note: "userInfo" is used instead of "credentials" because "credentials" contains "credential"
        // which is a sensitive pattern and would cause the entire object to be redacted
        var json = """{"name":"Test","userInfo":{"email":"user@test.com","password":"secret"}}""";

        // Act
        var redacted = _redactor.RedactJsonString(json);

        // Assert
        redacted.Should().Contain("[REDACTED]");
        redacted.Should().NotContain("secret");
        redacted.Should().Contain("user@test.com"); // email should not be redacted
        redacted.Should().Contain("Test");
    }

    [Fact]
    public void MaskForAudit_DeeplyNestedObject_ShouldRedactAtAllLevels()
    {
        // Arrange
        var request = new DeeplyNestedRequest
        {
            Level1 = new Level1Data
            {
                Value = "public",
                Level2 = new Level2Data
                {
                    Secret = "hidden",
                    Level3 = new Level3Data
                    {
                        Token = "deep-token"
                    }
                }
            }
        };

        // Act
        var masked = _redactor.MaskForAudit(request);

        // Assert
        masked.Level1!.Value.Should().Be("public");
        masked.Level1.Level2!.Secret.Should().Be(DefaultSensitiveDataRedactor.RedactedValue);
        masked.Level1.Level2.Level3!.Token.Should().Be(DefaultSensitiveDataRedactor.RedactedValue);
    }

    #endregion

    #region Array Redaction Tests

    [Fact]
    public void MaskForAudit_ArrayWithSensitiveFields_ShouldRedactInEachItem()
    {
        // Arrange
        var request = new ArrayRequest
        {
            Users =
            [
                new UserInfo { Name = "User1", Password = "pass1" },
                new UserInfo { Name = "User2", Password = "pass2" }
            ]
        };

        // Act
        var masked = _redactor.MaskForAudit(request);

        // Assert
        masked.Users.Should().HaveCount(2);
        masked.Users![0].Name.Should().Be("User1");
        masked.Users[0].Password.Should().Be(DefaultSensitiveDataRedactor.RedactedValue);
        masked.Users[1].Name.Should().Be("User2");
        masked.Users[1].Password.Should().Be(DefaultSensitiveDataRedactor.RedactedValue);
    }

    [Fact]
    public void MaskForAudit_NestedArrays_ShouldRedactAllItems()
    {
        // Arrange
        var request = new NestedArrayRequest
        {
            Groups =
            [
                new GroupInfo
                {
                    Name = "Group1",
                    Members =
                    [
                        new MemberInfo { Name = "Member1", ApiKey = "key1" }
                    ]
                }
            ]
        };

        // Act
        var masked = _redactor.MaskForAudit(request);

        // Assert
        masked.Groups.Should().NotBeNull();
        masked.Groups![0].Name.Should().Be("Group1");
        masked.Groups[0].Members.Should().NotBeNull();
        masked.Groups[0].Members![0].Name.Should().Be("Member1");
        masked.Groups[0].Members![0].ApiKey.Should().Be(DefaultSensitiveDataRedactor.RedactedValue);
    }

    #endregion

    #region Case Insensitive Matching Tests

    [Fact]
    public void MaskForAudit_CaseInsensitiveFieldName_UpperCase_ShouldRedact()
    {
        // The field name is "PASSWORD" (all caps) but should still match "password"
        var json = """{"PASSWORD":"secret","email":"test@test.com"}""";

        // Act
        var result = _redactor.RedactJsonString(json);

        // Assert
        result.Should().Contain("[REDACTED]");
        result.Should().Contain("test@test.com");
    }

    [Fact]
    public void MaskForAudit_CaseInsensitiveFieldName_MixedCase_ShouldRedact()
    {
        // Mixed case
        var json = """{"PaSsWoRd":"secret","name":"test"}""";

        // Act
        var result = _redactor.RedactJsonString(json);

        // Assert
        result.Should().Contain("[REDACTED]");
    }

    [Fact]
    public void MaskForAudit_ContainsMatch_UserPasswordField_ShouldRedact()
    {
        // Field "UserPassword" contains "password"
        var json = """{"UserPassword":"secret","name":"test"}""";

        // Act
        var result = _redactor.RedactJsonString(json);

        // Assert
        result.Should().Contain("[REDACTED]");
    }

    [Fact]
    public void MaskForAudit_ContainsMatch_PasswordHashField_ShouldRedact()
    {
        // Field "PasswordHash" contains "password"
        var json = """{"PasswordHash":"abc123hash","name":"test"}""";

        // Act
        var result = _redactor.RedactJsonString(json);

        // Assert
        result.Should().Contain("[REDACTED]");
    }

    #endregion

    #region Malformed JSON Tests

    [Fact]
    public void RedactJsonString_MalformedJson_ShouldReturnOriginal()
    {
        // Arrange
        var malformedJson = """{"password":"secret"""; // Missing closing brace

        // Act
        var result = _redactor.RedactJsonString(malformedJson);

        // Assert
        result.Should().Be(malformedJson);
    }

    [Fact]
    public void RedactJsonString_EmptyString_ShouldReturnOriginal()
    {
        // Act
        var result = _redactor.RedactJsonString("");

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void RedactJsonString_NullString_ShouldReturnNull()
    {
        // Act
        var result = _redactor.RedactJsonString(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void RedactJsonString_WhitespaceString_ShouldReturnOriginal()
    {
        // Act
        var result = _redactor.RedactJsonString("   ");

        // Assert
        result.Should().Be("   ");
    }

    [Fact]
    public void MaskForAudit_NonSerializableType_ShouldReturnOriginal()
    {
        // Arrange - A type that might cause serialization issues
        var request = new RequestWithCircularReference();
        request.Self = request; // Circular reference

        // Act - Should not throw, should return original
        var result = _redactor.MaskForAudit(request);

        // Assert
        result.Should().BeSameAs(request);
    }

    #endregion

    #region Additional Sensitive Fields Tests

    [Fact]
    public void MaskForAudit_WithAdditionalSensitiveFields_ShouldRedactThem()
    {
        // Arrange
        var request = new CustomFieldsRequest
        {
            Name = "Test",
            CustomSecret = "should-be-redacted",
            NormalField = "visible"
        };

        // Act
        var masked = _redactor.MaskForAudit(request, ["CustomSecret"]);

        // Assert
        masked.Name.Should().Be("Test");
        masked.CustomSecret.Should().Be(DefaultSensitiveDataRedactor.RedactedValue);
        masked.NormalField.Should().Be("visible");
    }

    [Fact]
    public void MaskForAudit_WithGlobalSensitiveFields_ShouldRedactThem()
    {
        // Arrange
        var options = Options.Create(new AuditOptions
        {
            GlobalSensitiveFields = ["CustomField", "DateOfBirth"]
        });
        var redactor = new DefaultSensitiveDataRedactor(options);
        var request = new GlobalFieldsRequest
        {
            Name = "Test",
            CustomField = "should-be-redacted",
            DateOfBirth = "1990-01-01"
        };

        // Act
        var masked = redactor.MaskForAudit(request);

        // Assert
        masked.Name.Should().Be("Test");
        masked.CustomField.Should().Be(DefaultSensitiveDataRedactor.RedactedValue);
        masked.DateOfBirth.Should().Be(DefaultSensitiveDataRedactor.RedactedValue);
    }

    #endregion

    #region Object Overload Tests

    [Fact]
    public void MaskForAudit_ObjectOverload_ShouldRedactSensitiveFields()
    {
        // Arrange
        object request = new SimpleRequest { Email = "test@test.com", Password = "secret" };

        // Act
        var masked = _redactor.MaskForAudit(request);

        // Assert
        var typedResult = masked.Should().BeOfType<SimpleRequest>().Subject;
        typedResult.Email.Should().Be("test@test.com");
        typedResult.Password.Should().Be(DefaultSensitiveDataRedactor.RedactedValue);
    }

    [Fact]
    public void MaskForAudit_ObjectOverloadWithAdditionalFields_ShouldRedactThem()
    {
        // Arrange
        object request = new CustomFieldsRequest
        {
            Name = "Test",
            CustomSecret = "secret",
            NormalField = "visible"
        };

        // Act
        var masked = _redactor.MaskForAudit(request, ["CustomSecret"]);

        // Assert
        var typedResult = masked.Should().BeOfType<CustomFieldsRequest>().Subject;
        typedResult.CustomSecret.Should().Be(DefaultSensitiveDataRedactor.RedactedValue);
    }

    #endregion

    #region Default Patterns Tests

    [Fact]
    public void DefaultSensitiveFieldPatterns_ShouldContainCommonSensitiveFields()
    {
        // Assert
        DefaultSensitiveDataRedactor.DefaultSensitiveFieldPatterns.Should().Contain("password");
        DefaultSensitiveDataRedactor.DefaultSensitiveFieldPatterns.Should().Contain("secret");
        DefaultSensitiveDataRedactor.DefaultSensitiveFieldPatterns.Should().Contain("token");
        DefaultSensitiveDataRedactor.DefaultSensitiveFieldPatterns.Should().Contain("apikey");
        DefaultSensitiveDataRedactor.DefaultSensitiveFieldPatterns.Should().Contain("ssn");
        DefaultSensitiveDataRedactor.DefaultSensitiveFieldPatterns.Should().Contain("creditcard");
        DefaultSensitiveDataRedactor.DefaultSensitiveFieldPatterns.Should().Contain("cvv");
    }

    [Fact]
    public void RedactedValue_ShouldBeCorrectString()
    {
        // Assert
        DefaultSensitiveDataRedactor.RedactedValue.Should().Be("[REDACTED]");
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new DefaultSensitiveDataRedactor(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    #endregion

    #region Test Types

    private sealed class SimpleRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    private sealed class TokenRequest
    {
        public string? UserId { get; set; }
        public string? Token { get; set; }
    }

    private sealed class ApiKeyRequest
    {
        public string? Name { get; set; }
        public string? ApiKey { get; set; }
    }

    private sealed class MultiSensitiveRequest
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Token { get; set; }
        public string? ApiKey { get; set; }
        public string? Ssn { get; set; }
    }

    private sealed class NestedRequest
    {
        public string? Name { get; set; }
        public UserInfoData? UserInfo { get; set; }
    }

    private sealed class UserInfoData
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    private sealed class DeeplyNestedRequest
    {
        public Level1Data? Level1 { get; set; }
    }

    private sealed class Level1Data
    {
        public string? Value { get; set; }
        public Level2Data? Level2 { get; set; }
    }

    private sealed class Level2Data
    {
        public string? Secret { get; set; }
        public Level3Data? Level3 { get; set; }
    }

    private sealed class Level3Data
    {
        public string? Token { get; set; }
    }

    private sealed class ArrayRequest
    {
        public UserInfo[]? Users { get; set; }
    }

    private sealed class UserInfo
    {
        public string? Name { get; set; }
        public string? Password { get; set; }
    }

    private sealed class NestedArrayRequest
    {
        public GroupInfo[]? Groups { get; set; }
    }

    private sealed class GroupInfo
    {
        public string? Name { get; set; }
        public MemberInfo[]? Members { get; set; }
    }

    private sealed class MemberInfo
    {
        public string? Name { get; set; }
        public string? ApiKey { get; set; }
    }

    private sealed class CustomFieldsRequest
    {
        public string? Name { get; set; }
        public string? CustomSecret { get; set; }
        public string? NormalField { get; set; }
    }

    private sealed class GlobalFieldsRequest
    {
        public string? Name { get; set; }
        public string? CustomField { get; set; }
        public string? DateOfBirth { get; set; }
    }

    private sealed class RequestWithCircularReference
    {
        public string? Name { get; set; }
        public RequestWithCircularReference? Self { get; set; }
    }

    #endregion
}
