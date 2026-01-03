using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Encina.Workflows.Tests;

/// <summary>
/// Shared fixture for workflow template tests.
/// Creates the YAML deserializer and resolves template paths once per test class.
/// </summary>
public class WorkflowTemplateFixture
{
    public IDeserializer YamlDeserializer { get; }
    public string TemplatesPath { get; }

    public WorkflowTemplateFixture()
    {
        YamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var testDir = AppContext.BaseDirectory;
        TemplatesPath = Path.Combine(testDir, "Workflows");

        if (!Directory.Exists(TemplatesPath))
        {
            TemplatesPath = FindTemplatesPath(testDir)
                ?? throw new InvalidOperationException($"Templates directory not found starting from {testDir}");
        }
    }

    private static string? FindTemplatesPath(string startDir)
    {
        var current = new DirectoryInfo(startDir);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, ".github", "workflows", "templates");
            if (Directory.Exists(candidate))
                return candidate;
            current = current.Parent;
        }
        return null;
    }
}

/// <summary>
/// Tests for validating GitHub Actions workflow templates.
/// These tests ensure the workflow YAML files are syntactically valid
/// and contain the expected structure.
/// </summary>
public class WorkflowTemplateTests : IClassFixture<WorkflowTemplateFixture>
{
    private readonly WorkflowTemplateFixture _fixture;

    public WorkflowTemplateTests(WorkflowTemplateFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void TemplatesDirectory_ShouldExist()
    {
        Directory.Exists(_fixture.TemplatesPath).ShouldBeTrue(
            $"Templates directory not found at: {_fixture.TemplatesPath}");
    }

    [Theory]
    [InlineData("encina-test.yml")]
    [InlineData("encina-matrix.yml")]
    [InlineData("encina-full-ci.yml")]
    public void Template_ShouldExist(string filename)
    {
        var path = Path.Combine(_fixture.TemplatesPath, filename);
        File.Exists(path).ShouldBeTrue($"Template file not found: {filename}");
    }

    [Theory]
    [InlineData("encina-test.yml")]
    [InlineData("encina-matrix.yml")]
    [InlineData("encina-full-ci.yml")]
    public void Template_ShouldBeValidYaml(string filename)
    {
        // Arrange
        var path = Path.Combine(_fixture.TemplatesPath, filename);
        var content = File.ReadAllText(path);

        // Act & Assert
        var exception = Record.Exception(() =>
            _fixture.YamlDeserializer.Deserialize<Dictionary<string, object>>(content));

        exception.ShouldBeNull($"YAML parsing failed for {filename}: {exception?.Message}");
    }

    [Theory]
    [InlineData("encina-test.yml")]
    [InlineData("encina-matrix.yml")]
    [InlineData("encina-full-ci.yml")]
    public void Template_ShouldHaveNameProperty(string filename)
    {
        // Arrange
        var workflow = ParseWorkflow(filename);

        // Assert
        workflow.ContainsKey("name").ShouldBeTrue($"{filename} should have a 'name' property");
        workflow["name"].ShouldNotBeNull();
    }

    [Theory]
    [InlineData("encina-test.yml")]
    [InlineData("encina-matrix.yml")]
    [InlineData("encina-full-ci.yml")]
    public void Template_ShouldBeReusableWorkflow(string filename)
    {
        // Arrange
        var workflow = ParseWorkflow(filename);

        // Assert - Should have 'on.workflow_call' trigger
        workflow.ContainsKey("on").ShouldBeTrue($"{filename} should have 'on' trigger");

        var onTrigger = workflow["on"] as Dictionary<object, object>;
        onTrigger.ShouldNotBeNull($"{filename} 'on' should be a dictionary");
        onTrigger!.ContainsKey("workflow_call").ShouldBeTrue(
            $"{filename} should have 'on.workflow_call' for reusable workflows");
    }

    [Theory]
    [InlineData("encina-test.yml")]
    [InlineData("encina-matrix.yml")]
    [InlineData("encina-full-ci.yml")]
    public void Template_ShouldHaveJobsSection(string filename)
    {
        // Arrange
        var workflow = ParseWorkflow(filename);

        // Assert
        workflow.ContainsKey("jobs").ShouldBeTrue($"{filename} should have 'jobs' section");

        var jobs = workflow["jobs"] as Dictionary<object, object>;
        jobs.ShouldNotBeNull($"{filename} 'jobs' should be a dictionary");
        jobs!.Count.ShouldBeGreaterThan(0, $"{filename} should have at least one job");
    }

    [Theory]
    [InlineData("encina-test.yml")]
    [InlineData("encina-matrix.yml")]
    [InlineData("encina-full-ci.yml")]
    public void Template_ShouldHavePermissionsSection(string filename)
    {
        // Arrange
        var workflow = ParseWorkflow(filename);

        // Assert
        workflow.ContainsKey("permissions").ShouldBeTrue(
            $"{filename} should have 'permissions' section for security");
    }

    [Theory]
    [InlineData("encina-test.yml", "solution")]
    [InlineData("encina-test.yml", "dotnet-version")]
    [InlineData("encina-test.yml", "coverage-threshold")]
    [InlineData("encina-matrix.yml", "test-os")]
    [InlineData("encina-matrix.yml", "test-databases")]
    [InlineData("encina-full-ci.yml", "run-integration-tests")]
    [InlineData("encina-full-ci.yml", "run-mutation-tests")]
    [InlineData("encina-full-ci.yml", "pack-nuget")]
    public void Template_ShouldHaveExpectedInput(string filename, string inputName)
    {
        // Arrange
        var workflow = ParseWorkflow(filename);
        var onTrigger = workflow["on"] as Dictionary<object, object>;
        onTrigger.ShouldNotBeNull();
        var workflowCall = onTrigger!["workflow_call"] as Dictionary<object, object>;
        workflowCall.ShouldNotBeNull();

        // Assert
        workflowCall!.ContainsKey("inputs").ShouldBeTrue(
            $"{filename} should have 'inputs' in workflow_call");

        var inputs = workflowCall["inputs"] as Dictionary<object, object>;
        inputs.ShouldNotBeNull();
        inputs!.ContainsKey(inputName).ShouldBeTrue(
            $"{filename} should have input '{inputName}'");
    }

    [Theory]
    [InlineData("encina-test.yml")]
    [InlineData("encina-matrix.yml")]
    [InlineData("encina-full-ci.yml")]
    public void Template_AllJobsShouldHaveRunsOn(string filename)
    {
        // Arrange
        var workflow = ParseWorkflow(filename);
        var jobs = workflow["jobs"] as Dictionary<object, object>;
        jobs.ShouldNotBeNull();

        // Assert
        foreach (var jobEntry in jobs!)
        {
            var jobName = jobEntry.Key.ToString();
            var job = jobEntry.Value as Dictionary<object, object>;
            job.ShouldNotBeNull();

            job!.ContainsKey("runs-on").ShouldBeTrue(
                $"{filename}: Job '{jobName}' should have 'runs-on' property");
        }
    }

    [Theory]
    [InlineData("encina-test.yml")]
    [InlineData("encina-matrix.yml")]
    [InlineData("encina-full-ci.yml")]
    public void Template_AllJobsShouldHaveSteps(string filename)
    {
        // Arrange
        var workflow = ParseWorkflow(filename);
        var jobs = workflow["jobs"] as Dictionary<object, object>;
        jobs.ShouldNotBeNull();

        // Assert
        foreach (var jobEntry in jobs!)
        {
            var jobName = jobEntry.Key.ToString();
            var job = jobEntry.Value as Dictionary<object, object>;
            job.ShouldNotBeNull();

            job!.ContainsKey("steps").ShouldBeTrue(
                $"{filename}: Job '{jobName}' should have 'steps'");

            var steps = job["steps"] as List<object>;
            steps.ShouldNotBeNull();
            steps!.Count.ShouldBeGreaterThan(0,
                $"{filename}: Job '{jobName}' should have at least one step");
        }
    }

    [Theory]
    [InlineData("encina-test.yml")]
    [InlineData("encina-matrix.yml")]
    [InlineData("encina-full-ci.yml")]
    public void Template_ShouldUseCheckoutAction(string filename)
    {
        // Arrange
        var workflow = ParseWorkflow(filename);

        // Assert
        WorkflowUsesAction(workflow, "actions/checkout")
            .ShouldBeTrue($"{filename} should use actions/checkout");
    }

    [Theory]
    [InlineData("encina-test.yml")]
    [InlineData("encina-matrix.yml")]
    [InlineData("encina-full-ci.yml")]
    public void Template_ShouldSetupDotnet(string filename)
    {
        // Arrange
        var workflow = ParseWorkflow(filename);

        // Assert
        WorkflowUsesAction(workflow, "actions/setup-dotnet")
            .ShouldBeTrue($"{filename} should use actions/setup-dotnet");
    }

    [Fact]
    public void EncinaTestTemplate_ShouldHaveDefaultTimeout()
    {
        // Arrange
        var workflow = ParseWorkflow("encina-test.yml");
        var onTrigger = workflow["on"] as Dictionary<object, object>;
        onTrigger.ShouldNotBeNull();
        var workflowCall = onTrigger!["workflow_call"] as Dictionary<object, object>;
        workflowCall.ShouldNotBeNull();
        var inputs = workflowCall!["inputs"] as Dictionary<object, object>;
        inputs.ShouldNotBeNull();
        var timeoutInput = inputs!["timeout-minutes"] as Dictionary<object, object>;
        timeoutInput.ShouldNotBeNull();

        // Assert
        timeoutInput!.ContainsKey("default").ShouldBeTrue();
    }

    [Fact]
    public void EncinaMatrixTemplate_ShouldHaveServiceContainers()
    {
        // Arrange
        var workflow = ParseWorkflow("encina-matrix.yml");
        var jobs = workflow["jobs"] as Dictionary<object, object>;
        jobs.ShouldNotBeNull("encina-matrix.yml should have a 'jobs' section");

        jobs!.ContainsKey("matrix-test").ShouldBeTrue(
            "encina-matrix.yml should have a job named 'matrix-test'");

        var matrixJob = jobs["matrix-test"] as Dictionary<object, object>;
        matrixJob.ShouldNotBeNull("'matrix-test' job should be a valid job definition");

        // Assert
        matrixJob!.ContainsKey("services").ShouldBeTrue(
            "encina-matrix.yml 'matrix-test' job should have service containers for databases");
    }

    [Fact]
    public void EncinaFullCiTemplate_ShouldHaveSecretsSection()
    {
        // Arrange
        var workflow = ParseWorkflow("encina-full-ci.yml");
        var onTrigger = workflow["on"] as Dictionary<object, object>;
        onTrigger.ShouldNotBeNull();
        var workflowCall = onTrigger!["workflow_call"] as Dictionary<object, object>;
        workflowCall.ShouldNotBeNull();

        // Assert
        workflowCall!.ContainsKey("secrets").ShouldBeTrue(
            "encina-full-ci.yml should have secrets section for NUGET_API_KEY");
    }

    [Fact]
    public void EncinaFullCiTemplate_ShouldHaveMultipleJobs()
    {
        // Arrange
        var workflow = ParseWorkflow("encina-full-ci.yml");
        var jobs = workflow["jobs"] as Dictionary<object, object>;
        jobs.ShouldNotBeNull();

        // Assert - Should have build, test, pack, etc.
        jobs!.Count.ShouldBeGreaterThan(3,
            "encina-full-ci.yml should have multiple jobs (build, test, pack, etc.)");

        jobs.ContainsKey("build").ShouldBeTrue();
        jobs.ContainsKey("unit-tests").ShouldBeTrue();
        jobs.ContainsKey("pack").ShouldBeTrue();
    }

    [Theory]
    [InlineData("encina-test.yml")]
    [InlineData("encina-matrix.yml")]
    [InlineData("encina-full-ci.yml")]
    public void Template_ShouldUseCacheAction(string filename)
    {
        // Arrange
        var workflow = ParseWorkflow(filename);

        // Assert
        WorkflowUsesAction(workflow, "actions/cache")
            .ShouldBeTrue($"{filename} should use actions/cache for NuGet packages");
    }

    [Theory]
    [InlineData("encina-test.yml")]
    [InlineData("encina-matrix.yml")]
    [InlineData("encina-full-ci.yml")]
    public void Template_ShouldHaveDocumentation(string filename)
    {
        // Arrange
        var path = Path.Combine(_fixture.TemplatesPath, filename);
        var content = File.ReadAllText(path);

        // Assert - Should have a header comment with usage
        content.ShouldStartWith("#");
        content.ShouldContain("Usage:");
    }

    private static bool WorkflowUsesAction(Dictionary<object, object> workflow, string actionPrefix)
    {
        if (!workflow.TryGetValue("jobs", out var jobsObj) || jobsObj is not Dictionary<object, object> jobs)
            return false;

        return jobs.Values
            .OfType<Dictionary<object, object>>()
            .Where(job => job.TryGetValue("steps", out var steps) && steps is List<object>)
            .SelectMany(job => ((List<object>)job["steps"]).OfType<Dictionary<object, object>>())
            .Any(step => step.TryGetValue("uses", out var uses) &&
                         uses?.ToString()?.StartsWith(actionPrefix, StringComparison.Ordinal) == true);
    }

    private Dictionary<object, object> ParseWorkflow(string filename)
    {
        var path = Path.Combine(_fixture.TemplatesPath, filename);
        var content = File.ReadAllText(path);
        return _fixture.YamlDeserializer.Deserialize<Dictionary<object, object>>(content);
    }
}
