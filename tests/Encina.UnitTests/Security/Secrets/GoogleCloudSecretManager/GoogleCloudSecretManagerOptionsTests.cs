using Encina.Security.Secrets.GoogleCloudSecretManager;
using Shouldly;

namespace Encina.UnitTests.Security.Secrets.GoogleCloudSecretManager;

public sealed class GoogleCloudSecretManagerOptionsTests
{
    [Fact]
    public void ProjectId_DefaultsToEmpty()
    {
        var options = new GoogleCloudSecretManagerOptions();

        options.ProjectId.ShouldBe("");
    }

    [Fact]
    public void ProjectId_CanBeSet()
    {
        var options = new GoogleCloudSecretManagerOptions();

        options.ProjectId = "my-gcp-project";

        options.ProjectId.ShouldBe("my-gcp-project");
    }
}
