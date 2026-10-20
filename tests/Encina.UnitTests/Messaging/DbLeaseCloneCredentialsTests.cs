using Microsoft.Data.SqlClient;

namespace Encina.UnitTests.Messaging;

/// <summary>
/// The inbox's independent writes run on <c>ICloneable.Clone()</c> of the request's connection (ADR-048); a
/// connection authenticated with an access token or a credential object instead of a connection string must keep
/// that authentication in the clone, otherwise the independent write could not open (and would fail closed).
/// </summary>
public sealed class DbLeaseCloneCredentialsTests
{
    private const string ConnectionString = "Server=tcp:localhost,1;Database=x;Encrypt=False";

    [Fact]
    public void SqlConnectionClone_CarriesTheAccessToken()
    {
        using var original = new SqlConnection(ConnectionString) { AccessToken = "token-value" };

        using var clone = (SqlConnection)((ICloneable)original).Clone();

        clone.AccessToken.ShouldBe("token-value");
    }

    [Fact]
    public void SqlConnectionClone_CarriesTheCredential()
    {
        var password = new System.Security.SecureString();
        foreach (var c in "pw") { password.AppendChar(c); }
        password.MakeReadOnly();
        using var original = new SqlConnection(ConnectionString) { Credential = new SqlCredential("user", password) };

        using var clone = (SqlConnection)((ICloneable)original).Clone();

        clone.Credential.ShouldNotBeNull();
        clone.Credential!.UserId.ShouldBe("user");
    }
}
