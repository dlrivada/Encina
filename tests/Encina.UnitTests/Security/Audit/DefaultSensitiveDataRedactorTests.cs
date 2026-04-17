using Encina.Security.Audit;
using Microsoft.Extensions.Options;
using Shouldly;

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
        masked.Email.ShouldBe("user@example.com");
        masked.Password.ShouldBe(DefaultSensitiveDataRedactor.RedactedValue);
    }

    [Fact]
    public void MaskForAudit_SimpleFieldWithToken_ShouldRedact()
    {
        // Arrange
        var request = new TokenRequest { UserId = "user-1", Token = "abc123" };

        // Act
        var masked = _redactor.MaskForAudit(request);

        // Assert
        masked.UserId.ShouldBe("user-1");
        masked.Token.ShouldBe(DefaultSensitiveDataRedactor.RedactedValue);
    }

    [Fact]
    public void MaskForAudit_SimpleFieldWithApiKey_ShouldRedact()
    {
        // Arrange
        var request = new ApiKeyRequest { Name = "Test", ApiKey = "key-12345" };

        // Act
        var masked = _redactor.MaskForAudit(request);

        // Assert
        masked.Name.ShouldBe("Test");
        masked.ApiKey.ShouldBe(DefaultSensitiveDataRedactor.RedactedValue);
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
        masked.Username.ShouldBe("testuser");
        masked.Password.ShouldBe(DefaultSensitiveDataRedactor.RedactedValue);
        masked.Token.ShouldBe(DefaultSensitiveDataRedactor.RedactedValue);
        masked.ApiKey.ShouldBe(DefaultSensitiveDataRedactor.RedactedValue);
        masked.Ssn.ShouldBe(DefaultSensitiveDataRedactor.RedactedValue);
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
        redacted.ShouldContain("[REDACTED]");
        redacted.ShouldNotContain("secret");
        redacted.ShouldContain("user@test.com"); // email should not be redacted
        redacted.ShouldContain("Test");
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
        masked.Level1!.Value.ShouldBe("public");
        masked.Level1.Level2!.Secret.ShouldBe(DefaultSensitiveDataRedactor.RedactedValue);
        masked.Level1.Level2.Level3!.Token.ShouldBe(DefaultSensitiveDataRedactor.RedactedValue);
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
        masked.Users!.Length.ShouldBe(2);
        masked.Users![0].Name.ShouldBe("User1");
        masked.Users[0].Password.ShouldBe(DefaultSensitiveDataRedactor.RedactedValue);
        masked.Users[1].Name.ShouldBe("User2");
        masked.Users[1].Password.ShouldBe(DefaultSensitiveDataRedactor.RedactedValue);
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
        masked.Groups.ShouldNotBeNull();
        masked.Groups![0].Name.ShouldBe("Group1");
        masked.Groups[0].Members.ShouldNotBeNull();
        masked.Groups[0].Members![0].Name.ShouldBe("Member1");
        masked.Groups[0].Members![0].ApiKey.ShouldBe(DefaultSensitiveDataRedactor.RedactedValue);
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
        result.ShouldContain("[REDACTED]");
        result.ShouldContain("test@test.com");
    }

    [Fact]
    public void MaskForAudit_CaseInsensitiveFieldName_MixedCase_ShouldRedact()
    {
        // Mixed case
        var json = """{"PaSsWoRd":"secret","name":"test"}""";

        // Act
        var result = _redactor.RedactJsonString(json);

        // Assert
        result.ShouldContain("[REDACTED]");
    }

    [Fact]
    public void MaskForAudit_ContainsMatch_UserPasswordField_ShouldRedact()
    {
        // Field "UserPassword" contains "password"
        var json = """{"UserPassword":"secret","name":"test"}""";

        // Act
        var result = _redactor.RedactJsonString(json);

        // Assert
        result.ShouldContain("[REDACTED]");
    }

    [Fact]
    public void MaskForAudit_ContainsMatch_PasswordHashField_ShouldRedact()
    {
        // Field "PasswordHash" contains "password"
        var json = """{"PasswordHash":"abc123hash","name":"test"}""";

        // Act
        var result = _redactor.RedactJsonString(json);

        // Assert
        result.ShouldContain("[REDACTED]");
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
        result.ShouldBe(malformedJson);
    }

    [Fact]
    public void RedactJsonString_EmptyString_ShouldReturnOriginal()
    {
        // Act
        var result = _redactor.RedactJsonString("");

        // Assert
        result.ShouldBe("");
    }

    [Fact]
    public void RedactJsonString_NullString_ShouldReturnNull()
    {
        // Act
        var result = _redactor.RedactJsonString(null!);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void RedactJsonString_WhitespaceString_ShouldReturnOriginal()
    {
        // Act
        var result = _redactor.RedactJsonString("   ");

        // Assert
        result.ShouldBe("   ");
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
        result.ShouldBeSameAs(request);
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
        masked.Name.ShouldBe("Test");
        masked.CustomSecret.ShouldBe(DefaultSensitiveDataRedactor.RedactedValue);
        masked.NormalField.ShouldBe("visible");
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
        masked.Name.ShouldBe("Test");
        masked.CustomField.ShouldBe(DefaultSensitiveDataRedactor.RedactedValue);
        masked.DateOfBirth.ShouldBe(DefaultSensitiveDataRedactor.RedactedValue);
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
        var typedResult = masked.ShouldBeOfType<SimpleRequest>();
        typedResult.Email.ShouldBe("test@test.com");
        typedResult.Password.ShouldBe(DefaultSensitiveDataRedactor.RedactedValue);
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
        var typedResult = masked.ShouldBeOfType<CustomFieldsRequest>();
        typedResult.CustomSecret.ShouldBe(DefaultSensitiveDataRedactor.RedactedValue);
    }

    #endregion

    #region Default Patterns Tests

    [Fact]
    public void DefaultSensitiveFieldPatterns_ShouldContainCommonSensitiveFields()
    {
        // Assert
        DefaultSensitiveDataRedactor.DefaultSensitiveFieldPatterns.ShouldContain("password");
        DefaultSensitiveDataRedactor.DefaultSensitiveFieldPatterns.ShouldContain("secret");
        DefaultSensitiveDataRedactor.DefaultSensitiveFieldPatterns.ShouldContain("token");
        DefaultSensitiveDataRedactor.DefaultSensitiveFieldPatterns.ShouldContain("apikey");
        DefaultSensitiveDataRedactor.DefaultSensitiveFieldPatterns.ShouldContain("ssn");
        DefaultSensitiveDataRedactor.DefaultSensitiveFieldPatterns.ShouldContain("creditcard");
        DefaultSensitiveDataRedactor.DefaultSensitiveFieldPatterns.ShouldContain("cvv");
    }

    [Fact]
    public void RedactedValue_ShouldBeCorrectString()
    {
        // Assert
        DefaultSensitiveDataRedactor.RedactedValue.ShouldBe("[REDACTED]");
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new DefaultSensitiveDataRedactor(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("options");
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
