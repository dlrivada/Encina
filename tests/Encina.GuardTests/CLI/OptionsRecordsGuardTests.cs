using Encina.Cli.Services;

using Shouldly;

using Xunit;

namespace Encina.GuardTests.CLI;

/// <summary>
/// Guard tests for <see cref="HandlerOptions"/>, <see cref="QueryOptions"/>,
/// <see cref="SagaOptions"/>, <see cref="NotificationOptions"/>,
/// <see cref="ProjectOptions"/>, and <see cref="GenerationResult"/>/<see cref="ScaffoldResult"/>
/// factories — ensures the public surface is stable and exercises assignment paths.
/// </summary>
public class OptionsRecordsGuardTests
{
    [Fact]
    public void HandlerOptions_Defaults_AreAssignable()
    {
        var options = new HandlerOptions
        {
            Name = "DoThing",
            OutputDirectory = "/tmp"
        };

        options.Name.ShouldBe("DoThing");
        options.ResponseType.ShouldBe("Unit");
        options.OutputDirectory.ShouldBe("/tmp");
        options.Namespace.ShouldBeNull();
    }

    [Fact]
    public void HandlerOptions_AllPropertiesSettable()
    {
        var options = new HandlerOptions
        {
            Name = "N",
            ResponseType = "R",
            OutputDirectory = "O",
            Namespace = "NS"
        };

        options.Name.ShouldBe("N");
        options.ResponseType.ShouldBe("R");
        options.OutputDirectory.ShouldBe("O");
        options.Namespace.ShouldBe("NS");
    }

    [Fact]
    public void QueryOptions_AllPropertiesSettable()
    {
        var options = new QueryOptions
        {
            Name = "GetX",
            ResponseType = "Dto",
            OutputDirectory = "O",
            Namespace = "NS"
        };

        options.Name.ShouldBe("GetX");
        options.ResponseType.ShouldBe("Dto");
        options.OutputDirectory.ShouldBe("O");
        options.Namespace.ShouldBe("NS");
    }

    [Fact]
    public void SagaOptions_AllPropertiesSettable()
    {
        var options = new SagaOptions
        {
            Name = "Order",
            Steps = ["A", "B"],
            OutputDirectory = "O",
            Namespace = "NS"
        };

        options.Name.ShouldBe("Order");
        options.Steps.Count.ShouldBe(2);
        options.OutputDirectory.ShouldBe("O");
        options.Namespace.ShouldBe("NS");
    }

    [Fact]
    public void NotificationOptions_AllPropertiesSettable()
    {
        var options = new NotificationOptions
        {
            Name = "OrderCreated",
            OutputDirectory = "O",
            Namespace = "NS"
        };

        options.Name.ShouldBe("OrderCreated");
        options.OutputDirectory.ShouldBe("O");
        options.Namespace.ShouldBe("NS");
    }

    [Fact]
    public void ProjectOptions_DefaultsAndOptionals()
    {
        var options = new ProjectOptions
        {
            Template = "api",
            Name = "Sample",
            OutputDirectory = "/tmp/sample"
        };

        options.Template.ShouldBe("api");
        options.Name.ShouldBe("Sample");
        options.OutputDirectory.ShouldBe("/tmp/sample");
        options.Database.ShouldBeNull();
        options.Caching.ShouldBeNull();
        options.Transport.ShouldBeNull();
        options.Force.ShouldBeFalse();
    }

    [Fact]
    public void ProjectOptions_AllPropertiesSettable()
    {
        var options = new ProjectOptions
        {
            Template = "worker",
            Name = "Full",
            OutputDirectory = "/tmp",
            Database = "sqlserver",
            Caching = "redis",
            Transport = "kafka",
            Force = true
        };

        options.Template.ShouldBe("worker");
        options.Database.ShouldBe("sqlserver");
        options.Caching.ShouldBe("redis");
        options.Transport.ShouldBe("kafka");
        options.Force.ShouldBeTrue();
    }

    // ─── Result factories ───

    [Fact]
    public void ScaffoldResult_Ok_HoldsFiles()
    {
        var files = new[] { "a.cs", "b.cs" };
        var result = ScaffoldResult.Ok(files);

        result.Success.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
        result.GeneratedFiles.Count.ShouldBe(2);
    }

    [Fact]
    public void ScaffoldResult_Error_HoldsMessage()
    {
        var result = ScaffoldResult.Error("oops");

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("oops");
        result.GeneratedFiles.ShouldBeEmpty();
    }

    [Fact]
    public void GenerationResult_Ok_HoldsFiles()
    {
        var files = new[] { "x.cs" };
        var result = GenerationResult.Ok(files);

        result.Success.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
        result.GeneratedFiles.Count.ShouldBe(1);
    }

    [Fact]
    public void GenerationResult_Error_HoldsMessage()
    {
        var result = GenerationResult.Error("bad");

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("bad");
        result.GeneratedFiles.ShouldBeEmpty();
    }

    [Fact]
    public void PackageResult_Ok_HasNoError()
    {
        var result = PackageResult.Ok();

        result.Success.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void PackageResult_Error_HoldsMessage()
    {
        var result = PackageResult.Error("err");

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("err");
    }

    [Fact]
    public void InstalledPackage_PropertiesAssignable()
    {
        var pkg = new InstalledPackage { Name = "Encina", Version = "1.0" };

        pkg.Name.ShouldBe("Encina");
        pkg.Version.ShouldBe("1.0");
    }
}
