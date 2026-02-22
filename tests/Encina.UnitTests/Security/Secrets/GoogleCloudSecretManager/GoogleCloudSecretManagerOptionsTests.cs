using Encina.Security.Secrets.GoogleCloudSecretManager;
using FluentAssertions;

namespace Encina.UnitTests.Security.Secrets.GoogleCloudSecretManager;

public sealed class GoogleCloudSecretManagerOptionsTests
{
    [Fact]
    public void ProjectId_DefaultsToEmpty()
    {
        var options = new GoogleCloudSecretManagerOptions();

        options.ProjectId.Should().Be("");
    }

    [Fact]
    public void ProjectId_CanBeSet()
    {
        var options = new GoogleCloudSecretManagerOptions();

        options.ProjectId = "my-gcp-project";

        options.ProjectId.Should().Be("my-gcp-project");
    }
}
