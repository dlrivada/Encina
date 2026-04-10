using Encina.Cli.Services;

using Shouldly;

using Xunit;

namespace Encina.GuardTests.CLI;

/// <summary>
/// Guard tests that exercise the happy-path branches of <see cref="CodeGenerator"/> to
/// produce real line coverage for the guard flag. These complement the pure guard-clause
/// tests in <see cref="CodeGeneratorGuardTests"/> by running each public method end-to-end
/// with valid inputs against a disposable temporary directory.
/// </summary>
public class CodeGeneratorHappyPathGuardTests : IDisposable
{
    private readonly string _tempDir;

    public CodeGeneratorHappyPathGuardTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"encina-cli-gcli-hp-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    // ─── Command handler generation ───

    [Fact]
    public async Task GenerateCommandHandlerAsync_UnitResponse_EmitsCommandAndHandler()
    {
        var options = new HandlerOptions
        {
            Name = "DoWork",
            ResponseType = "Unit",
            OutputDirectory = _tempDir,
            Namespace = "Sample.Commands"
        };

        var result = await CodeGenerator.GenerateCommandHandlerAsync(options);

        result.Success.ShouldBeTrue();
        result.GeneratedFiles.Count.ShouldBe(2);
        var commandPath = Path.Combine(_tempDir, "DoWork.cs");
        var handlerPath = Path.Combine(_tempDir, "DoWorkHandler.cs");
        File.Exists(commandPath).ShouldBeTrue();
        File.Exists(handlerPath).ShouldBeTrue();
        (await File.ReadAllTextAsync(commandPath)).ShouldContain("public sealed record DoWork : ICommand");
        (await File.ReadAllTextAsync(handlerPath)).ShouldContain("ICommandHandler<DoWork>");
    }

    [Fact]
    public async Task GenerateCommandHandlerAsync_UnitLowercaseResponse_StillUnit()
    {
        var options = new HandlerOptions
        {
            Name = "DoLower",
            ResponseType = "unit",
            OutputDirectory = _tempDir,
            Namespace = "Sample"
        };

        var result = await CodeGenerator.GenerateCommandHandlerAsync(options);

        result.Success.ShouldBeTrue();
        (await File.ReadAllTextAsync(Path.Combine(_tempDir, "DoLowerHandler.cs"))).ShouldContain("Unit.Default");
    }

    [Fact]
    public async Task GenerateCommandHandlerAsync_TypedResponse_UsesResponseType()
    {
        var options = new HandlerOptions
        {
            Name = "CreateAccount",
            ResponseType = "AccountId",
            OutputDirectory = _tempDir,
            Namespace = "Sample"
        };

        var result = await CodeGenerator.GenerateCommandHandlerAsync(options);

        result.Success.ShouldBeTrue();
        (await File.ReadAllTextAsync(Path.Combine(_tempDir, "CreateAccount.cs"))).ShouldContain("ICommand<AccountId>");
        (await File.ReadAllTextAsync(Path.Combine(_tempDir, "CreateAccountHandler.cs"))).ShouldContain("ICommandHandler<CreateAccount, AccountId>");
    }

    [Fact]
    public async Task GenerateCommandHandlerAsync_NoExplicitNamespace_DetectsFromParentCsproj()
    {
        // Place a csproj in the parent directory so DetectNamespace walks up and composes.
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Parent.csproj"), "<Project />");
        var sub = Path.Combine(_tempDir, "Commands");
        Directory.CreateDirectory(sub);

        var options = new HandlerOptions
        {
            Name = "Foo",
            OutputDirectory = sub
        };

        var result = await CodeGenerator.GenerateCommandHandlerAsync(options);

        result.Success.ShouldBeTrue();
        (await File.ReadAllTextAsync(Path.Combine(sub, "Foo.cs"))).ShouldContain("namespace Parent.Commands;");
    }

    [Fact]
    public async Task GenerateCommandHandlerAsync_NoExplicitNamespace_DetectsFromSameFolderCsproj()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Here.csproj"), "<Project />");

        var options = new HandlerOptions
        {
            Name = "Bar",
            OutputDirectory = _tempDir
        };

        var result = await CodeGenerator.GenerateCommandHandlerAsync(options);

        result.Success.ShouldBeTrue();
        (await File.ReadAllTextAsync(Path.Combine(_tempDir, "Bar.cs"))).ShouldContain("namespace Here;");
    }

    // ─── Query handler generation ───

    [Fact]
    public async Task GenerateQueryHandlerAsync_ValidOptions_EmitsQueryAndHandler()
    {
        var options = new QueryOptions
        {
            Name = "GetUserById",
            ResponseType = "UserDto",
            OutputDirectory = _tempDir,
            Namespace = "Sample.Queries"
        };

        var result = await CodeGenerator.GenerateQueryHandlerAsync(options);

        result.Success.ShouldBeTrue();
        result.GeneratedFiles.Count.ShouldBe(2);
        (await File.ReadAllTextAsync(Path.Combine(_tempDir, "GetUserById.cs"))).ShouldContain("IQuery<UserDto>");
        (await File.ReadAllTextAsync(Path.Combine(_tempDir, "GetUserByIdHandler.cs"))).ShouldContain("IQueryHandler<GetUserById, UserDto>");
    }

    [Fact]
    public async Task GenerateQueryHandlerAsync_NoExplicitNamespace_Works()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Q.csproj"), "<Project />");

        var options = new QueryOptions
        {
            Name = "GetSomething",
            ResponseType = "Something",
            OutputDirectory = _tempDir
        };

        var result = await CodeGenerator.GenerateQueryHandlerAsync(options);

        result.Success.ShouldBeTrue();
    }

    // ─── Saga generation ───

    [Fact]
    public async Task GenerateSagaAsync_ValidOptions_EmitsDataAndSaga()
    {
        var options = new SagaOptions
        {
            Name = "OrderFlow",
            Steps = ["Validate", "Charge", "Ship", "Notify"],
            OutputDirectory = _tempDir,
            Namespace = "Sample.Sagas"
        };

        var result = await CodeGenerator.GenerateSagaAsync(options);

        result.Success.ShouldBeTrue();
        result.GeneratedFiles.Count.ShouldBe(2);
        var sagaContent = await File.ReadAllTextAsync(Path.Combine(_tempDir, "OrderFlowSaga.cs"));
        sagaContent.ShouldContain(".Step(\"Validate\")");
        sagaContent.ShouldContain(".Step(\"Charge\")");
        sagaContent.ShouldContain(".Step(\"Ship\")");
        sagaContent.ShouldContain(".Step(\"Notify\")");
        (await File.ReadAllTextAsync(Path.Combine(_tempDir, "OrderFlowData.cs"))).ShouldContain("CorrelationId");
    }

    [Fact]
    public async Task GenerateSagaAsync_SingleStep_Works()
    {
        var options = new SagaOptions
        {
            Name = "SingleStep",
            Steps = ["Only"],
            OutputDirectory = _tempDir,
            Namespace = "Sample"
        };

        var result = await CodeGenerator.GenerateSagaAsync(options);

        result.Success.ShouldBeTrue();
        (await File.ReadAllTextAsync(Path.Combine(_tempDir, "SingleStepSaga.cs"))).ShouldContain(".Step(\"Only\")");
    }

    [Fact]
    public async Task GenerateSagaAsync_NoExplicitNamespace_UsesDetection()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "S.csproj"), "<Project />");

        var options = new SagaOptions
        {
            Name = "DetectNs",
            Steps = ["A"],
            OutputDirectory = _tempDir
        };

        var result = await CodeGenerator.GenerateSagaAsync(options);

        result.Success.ShouldBeTrue();
    }

    // ─── Notification generation ───

    [Fact]
    public async Task GenerateNotificationAsync_ValidOptions_EmitsFiles()
    {
        var options = new NotificationOptions
        {
            Name = "OrderPlaced",
            OutputDirectory = _tempDir,
            Namespace = "Sample.Events"
        };

        var result = await CodeGenerator.GenerateNotificationAsync(options);

        result.Success.ShouldBeTrue();
        result.GeneratedFiles.Count.ShouldBe(2);
        (await File.ReadAllTextAsync(Path.Combine(_tempDir, "OrderPlaced.cs"))).ShouldContain("INotification");
        (await File.ReadAllTextAsync(Path.Combine(_tempDir, "OrderPlacedHandler.cs"))).ShouldContain("INotificationHandler<OrderPlaced>");
    }

    [Fact]
    public async Task GenerateNotificationAsync_NoExplicitNamespace_Works()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "N.csproj"), "<Project />");

        var options = new NotificationOptions
        {
            Name = "Notified",
            OutputDirectory = _tempDir
        };

        var result = await CodeGenerator.GenerateNotificationAsync(options);

        result.Success.ShouldBeTrue();
    }

    // ─── Stryker config generation ───

    [Fact]
    public async Task GenerateStrykerConfigAsync_BasicConfig_Succeeds()
    {
        var options = new StrykerOptions
        {
            ProjectPath = "src/MyApp/MyApp.csproj",
            OutputDirectory = _tempDir
        };

        var result = await CodeGenerator.GenerateStrykerConfigAsync(options);

        result.Success.ShouldBeTrue();
        var content = await File.ReadAllTextAsync(Path.Combine(_tempDir, "stryker-config.json"));
        content.ShouldContain("\"high\": 80");
        content.ShouldContain("\"low\": 60");
        content.ShouldContain("\"break\": 50");
        content.ShouldNotContain("baseline");
    }

    [Fact]
    public async Task GenerateStrykerConfigAsync_AdvancedConfig_IncludesBaselineAndSince()
    {
        var options = new StrykerOptions
        {
            ProjectPath = "src/MyApp/MyApp.csproj",
            OutputDirectory = _tempDir,
            UseAdvanced = true
        };

        var result = await CodeGenerator.GenerateStrykerConfigAsync(options);

        result.Success.ShouldBeTrue();
        var content = await File.ReadAllTextAsync(Path.Combine(_tempDir, "stryker-config.json"));
        content.ShouldContain("baseline");
        content.ShouldContain("\"since\"");
        content.ShouldContain("ignore-methods");
    }

    [Fact]
    public async Task GenerateStrykerConfigAsync_CustomThresholds_Work()
    {
        var options = new StrykerOptions
        {
            ProjectPath = "src/MyApp/MyApp.csproj",
            OutputDirectory = _tempDir,
            ThresholdHigh = 95,
            ThresholdLow = 75,
            ThresholdBreak = 60
        };

        var result = await CodeGenerator.GenerateStrykerConfigAsync(options);

        result.Success.ShouldBeTrue();
        var content = await File.ReadAllTextAsync(Path.Combine(_tempDir, "stryker-config.json"));
        content.ShouldContain("\"high\": 95");
        content.ShouldContain("\"low\": 75");
        content.ShouldContain("\"break\": 60");
    }

    [Fact]
    public async Task GenerateStrykerConfigAsync_ExplicitTestProjects_EmittedInConfig()
    {
        var options = new StrykerOptions
        {
            ProjectPath = "src/MyApp/MyApp.csproj",
            TestProjects = ["tests/MyApp.Tests/MyApp.Tests.csproj", "tests/MyApp.Other/MyApp.Other.csproj"],
            OutputDirectory = _tempDir
        };

        var result = await CodeGenerator.GenerateStrykerConfigAsync(options);

        result.Success.ShouldBeTrue();
        var content = await File.ReadAllTextAsync(Path.Combine(_tempDir, "stryker-config.json"));
        content.ShouldContain("MyApp.Tests.csproj");
        content.ShouldContain("MyApp.Other.csproj");
    }

    [Fact]
    public async Task GenerateStrykerConfigAsync_ProjectPathWithSrcSegment_DerivesTestPath()
    {
        var options = new StrykerOptions
        {
            ProjectPath = "src/MyApp/MyApp.csproj",
            OutputDirectory = _tempDir
        };

        var result = await CodeGenerator.GenerateStrykerConfigAsync(options);

        result.Success.ShouldBeTrue();
        var content = await File.ReadAllTextAsync(Path.Combine(_tempDir, "stryker-config.json"));
        content.ShouldContain("tests/MyApp/MyApp.Tests/MyApp.Tests.csproj");
    }

    [Fact]
    public async Task GenerateStrykerConfigAsync_ProjectPathWithoutSrcSegment_FallsBack()
    {
        var options = new StrykerOptions
        {
            ProjectPath = "Apps/Widget/Widget.csproj",
            OutputDirectory = _tempDir
        };

        var result = await CodeGenerator.GenerateStrykerConfigAsync(options);

        result.Success.ShouldBeTrue();
        var content = await File.ReadAllTextAsync(Path.Combine(_tempDir, "stryker-config.json"));
        content.ShouldContain("Apps/Widget/Widget.csproj");
    }

    [Theory]
    [InlineData(80, 60, 70)] // break > low
    [InlineData(60, 80, 50)] // low > high
    [InlineData(80, 60, -1)] // break < 0
    [InlineData(101, 60, 50)] // high > 100
    public async Task GenerateStrykerConfigAsync_InvalidThresholds_ReturnError(int high, int low, int @break)
    {
        var options = new StrykerOptions
        {
            ProjectPath = "src/X/X.csproj",
            OutputDirectory = _tempDir,
            ThresholdHigh = high,
            ThresholdLow = low,
            ThresholdBreak = @break
        };

        var result = await CodeGenerator.GenerateStrykerConfigAsync(options);

        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Invalid thresholds");
    }
}
