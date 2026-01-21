using Encina.Modules.Isolation;

namespace Encina.UnitTests.Core.Modules.Isolation;

/// <summary>
/// Unit tests for <see cref="ModuleExecutionContext"/>.
/// </summary>
public class ModuleExecutionContextTests
{
    #region CurrentModule

    [Fact]
    public void CurrentModule_Initially_ShouldBeNull()
    {
        // Arrange
        var context = new ModuleExecutionContext();

        // Act
        var result = context.CurrentModule;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void CurrentModule_AfterSetModule_ShouldReturnModuleName()
    {
        // Arrange
        var context = new ModuleExecutionContext();

        // Act
        context.SetModule("Orders");

        // Assert
        context.CurrentModule.ShouldBe("Orders");
    }

    #endregion

    #region SetModule

    [Fact]
    public void SetModule_ValidModuleName_ShouldSetCurrentModule()
    {
        // Arrange
        var context = new ModuleExecutionContext();

        // Act
        context.SetModule("Payments");

        // Assert
        context.CurrentModule.ShouldBe("Payments");
    }

    [Fact]
    public void SetModule_CalledTwice_ShouldOverwritePreviousModule()
    {
        // Arrange
        var context = new ModuleExecutionContext();
        context.SetModule("Orders");

        // Act
        context.SetModule("Payments");

        // Assert
        context.CurrentModule.ShouldBe("Payments");
    }

    [Fact]
    public void SetModule_NullModuleName_ShouldThrow()
    {
        // Arrange
        var context = new ModuleExecutionContext();

        // Act & Assert
        Should.Throw<ArgumentException>(() => context.SetModule(null!));
    }

    [Fact]
    public void SetModule_EmptyModuleName_ShouldThrow()
    {
        // Arrange
        var context = new ModuleExecutionContext();

        // Act & Assert
        Should.Throw<ArgumentException>(() => context.SetModule(""));
    }

    [Fact]
    public void SetModule_WhitespaceModuleName_ShouldThrow()
    {
        // Arrange
        var context = new ModuleExecutionContext();

        // Act & Assert
        Should.Throw<ArgumentException>(() => context.SetModule("   "));
    }

    #endregion

    #region ClearModule

    [Fact]
    public void ClearModule_WhenModuleSet_ShouldClearCurrentModule()
    {
        // Arrange
        var context = new ModuleExecutionContext();
        context.SetModule("Orders");

        // Act
        context.ClearModule();

        // Assert
        context.CurrentModule.ShouldBeNull();
    }

    [Fact]
    public void ClearModule_WhenNoModuleSet_ShouldNotThrow()
    {
        // Arrange
        var context = new ModuleExecutionContext();

        // Act & Assert
        Should.NotThrow(() => context.ClearModule());
        context.CurrentModule.ShouldBeNull();
    }

    #endregion

    #region CreateScope

    [Fact]
    public void CreateScope_ValidModuleName_ShouldSetCurrentModule()
    {
        // Arrange
        var context = new ModuleExecutionContext();

        // Act
        using (context.CreateScope("Orders"))
        {
            // Assert (inside scope)
            context.CurrentModule.ShouldBe("Orders");
        }
    }

    [Fact]
    public void CreateScope_WhenDisposed_ShouldClearModule()
    {
        // Arrange
        var context = new ModuleExecutionContext();

        // Act
        using (context.CreateScope("Orders"))
        {
            // Inside scope
        }

        // Assert (after scope)
        context.CurrentModule.ShouldBeNull();
    }

    [Fact]
    public void CreateScope_NestedScopes_ShouldRestorePreviousModule()
    {
        // Arrange
        var context = new ModuleExecutionContext();

        // Act & Assert
        using (context.CreateScope("Orders"))
        {
            context.CurrentModule.ShouldBe("Orders");

            using (context.CreateScope("Payments"))
            {
                context.CurrentModule.ShouldBe("Payments");
            }

            // Should restore to Orders
            context.CurrentModule.ShouldBe("Orders");
        }

        // Should be null after all scopes
        context.CurrentModule.ShouldBeNull();
    }

    [Fact]
    public void CreateScope_TripleNestedScopes_ShouldRestoreCorrectly()
    {
        // Arrange
        var context = new ModuleExecutionContext();

        // Act & Assert
        using (context.CreateScope("Module1"))
        {
            context.CurrentModule.ShouldBe("Module1");

            using (context.CreateScope("Module2"))
            {
                context.CurrentModule.ShouldBe("Module2");

                using (context.CreateScope("Module3"))
                {
                    context.CurrentModule.ShouldBe("Module3");
                }

                context.CurrentModule.ShouldBe("Module2");
            }

            context.CurrentModule.ShouldBe("Module1");
        }

        context.CurrentModule.ShouldBeNull();
    }

    [Fact]
    public void CreateScope_NullModuleName_ShouldThrow()
    {
        // Arrange
        var context = new ModuleExecutionContext();

        // Act & Assert
        Should.Throw<ArgumentException>(() => context.CreateScope(null!));
    }

    [Fact]
    public void CreateScope_EmptyModuleName_ShouldThrow()
    {
        // Arrange
        var context = new ModuleExecutionContext();

        // Act & Assert
        Should.Throw<ArgumentException>(() => context.CreateScope(""));
    }

    [Fact]
    public void CreateScope_WhitespaceModuleName_ShouldThrow()
    {
        // Arrange
        var context = new ModuleExecutionContext();

        // Act & Assert
        Should.Throw<ArgumentException>(() => context.CreateScope("   "));
    }

    [Fact]
    public void CreateScope_DisposeCalledTwice_ShouldNotThrow()
    {
        // Arrange
        var context = new ModuleExecutionContext();
        var scope = context.CreateScope("Orders");

        // Act
        scope.Dispose();

        // Assert
        Should.NotThrow(() => scope.Dispose());
    }

    [Fact]
    public void CreateScope_DisposeCalledTwice_ShouldNotAffectModule()
    {
        // Arrange
        var context = new ModuleExecutionContext();

        using (context.CreateScope("Outer"))
        {
            var innerScope = context.CreateScope("Inner");
            innerScope.Dispose();

            // Should be back to Outer
            context.CurrentModule.ShouldBe("Outer");

            // Calling dispose again should not change anything
            innerScope.Dispose();
            context.CurrentModule.ShouldBe("Outer");
        }
    }

    #endregion

    #region AsyncLocal Behavior

    [Fact]
    public async Task CurrentModule_ShouldFlowAcrossAwait()
    {
        // Arrange
        var context = new ModuleExecutionContext();
        context.SetModule("Orders");

        // Act
        await Task.Yield();

        // Assert
        context.CurrentModule.ShouldBe("Orders");
    }

    [Fact]
    public async Task CurrentModule_ShouldFlowAcrossTaskRun()
    {
        // Arrange
        var context = new ModuleExecutionContext();
        context.SetModule("Orders");
        string? moduleInsideTask = null;

        // Act
        await Task.Run(() =>
        {
            moduleInsideTask = context.CurrentModule;
        });

        // Assert
        moduleInsideTask.ShouldBe("Orders");
    }

    [Fact]
    public async Task CreateScope_ShouldFlowAcrossAwait()
    {
        // Arrange
        var context = new ModuleExecutionContext();
        string? moduleBeforeAwait;
        string? moduleAfterAwait;

        // Act
        using (context.CreateScope("Orders"))
        {
            moduleBeforeAwait = context.CurrentModule;
            await Task.Yield();
            moduleAfterAwait = context.CurrentModule;
        }

        // Assert
        moduleBeforeAwait.ShouldBe("Orders");
        moduleAfterAwait.ShouldBe("Orders");
    }

    [Fact]
    public async Task CreateScope_ShouldFlowIntoNestedTasks()
    {
        // Arrange
        var context = new ModuleExecutionContext();
        string? moduleInTask1 = null;
        string? moduleInTask2 = null;

        // Act
        using (context.CreateScope("Orders"))
        {
            await Task.Run(async () =>
            {
                moduleInTask1 = context.CurrentModule;
                await Task.Run(() =>
                {
                    moduleInTask2 = context.CurrentModule;
                });
            });
        }

        // Assert
        moduleInTask1.ShouldBe("Orders");
        moduleInTask2.ShouldBe("Orders");
    }

    [Fact]
    public async Task ParallelTasks_ShouldHaveIndependentContexts()
    {
        // Arrange
        var context = new ModuleExecutionContext();
        var results = new System.Collections.Concurrent.ConcurrentDictionary<string, string?>();

        // Act
        var task1 = Task.Run(async () =>
        {
            context.SetModule("Orders");
            await Task.Delay(50);
            results["Task1"] = context.CurrentModule;
        });

        var task2 = Task.Run(async () =>
        {
            context.SetModule("Payments");
            await Task.Delay(50);
            results["Task2"] = context.CurrentModule;
        });

        await Task.WhenAll(task1, task2);

        // Assert - Each task should see its own module
        results["Task1"].ShouldBe("Orders");
        results["Task2"].ShouldBe("Payments");
    }

    [Fact]
    public async Task ParallelTasksWithScopes_ShouldBeIsolated()
    {
        // Arrange
        var context = new ModuleExecutionContext();
        var results = new System.Collections.Concurrent.ConcurrentDictionary<string, string?>();

        // Act
        var tasks = Enumerable.Range(1, 10).Select(i => Task.Run(async () =>
        {
            using (context.CreateScope($"Module{i}"))
            {
                await Task.Delay(Random.Shared.Next(10, 50));
                results[$"Task{i}"] = context.CurrentModule;
            }
        }));

        await Task.WhenAll(tasks);

        // Assert - Each task should have seen its own module
        for (int i = 1; i <= 10; i++)
        {
            results[$"Task{i}"].ShouldBe($"Module{i}");
        }
    }

    [Fact]
    public async Task ConfigureAwait_False_ShouldStillFlowContext()
    {
        // Arrange
        var context = new ModuleExecutionContext();
        context.SetModule("Orders");

        // Act
        await Task.Delay(10).ConfigureAwait(false);

        // Assert
        context.CurrentModule.ShouldBe("Orders");
    }

    [Fact]
    public async Task MultipleScopesInSequence_ShouldWorkCorrectly()
    {
        // Arrange
        var context = new ModuleExecutionContext();
        var modules = new List<string?>();

        // Act
        using (context.CreateScope("Orders"))
        {
            modules.Add(context.CurrentModule);
            await Task.Yield();
        }

        using (context.CreateScope("Payments"))
        {
            modules.Add(context.CurrentModule);
            await Task.Yield();
        }

        using (context.CreateScope("Inventory"))
        {
            modules.Add(context.CurrentModule);
            await Task.Yield();
        }

        modules.Add(context.CurrentModule);

        // Assert
        modules.ShouldBe(["Orders", "Payments", "Inventory", null]);
    }

    #endregion

    #region Thread Safety

    [Fact]
    public async Task ConcurrentSetAndClear_ShouldNotCorruptState()
    {
        // Arrange
        var context = new ModuleExecutionContext();
        var iterations = 1000;
        var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Act
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            try
            {
                for (int i = 0; i < iterations; i++)
                {
                    context.SetModule($"Module{i}");
                    await Task.Yield();
                    var current = context.CurrentModule;
                    // The module should not be null inside this task
                    if (current is null)
                    {
                        errors.Add(new InvalidOperationException("Module was unexpectedly null"));
                    }
                    context.ClearModule();
                }
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        }));

        await Task.WhenAll(tasks);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ConcurrentScopes_ShouldMaintainCorrectState()
    {
        // Arrange
        var context = new ModuleExecutionContext();
        var errors = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act
        var tasks = Enumerable.Range(0, 10).Select(taskId => Task.Run(async () =>
        {
            for (int i = 0; i < 100; i++)
            {
                var expectedModule = $"Task{taskId}_Iteration{i}";
                using (context.CreateScope(expectedModule))
                {
                    await Task.Yield();
                    var actual = context.CurrentModule;
                    if (actual != expectedModule)
                    {
                        errors.Add($"Expected {expectedModule} but got {actual}");
                    }
                }
            }
        }));

        await Task.WhenAll(tasks);

        // Assert
        errors.ShouldBeEmpty();
    }

    #endregion

    #region Static AsyncLocal Isolation

    [Fact]
    public void MultipleInstances_ShouldShareSameAsyncLocal()
    {
        // Arrange - ModuleExecutionContext uses static AsyncLocal
        var context1 = new ModuleExecutionContext();
        var context2 = new ModuleExecutionContext();

        // Act
        context1.SetModule("Orders");

        // Assert - Both instances see the same value because AsyncLocal is static
        context1.CurrentModule.ShouldBe("Orders");
        context2.CurrentModule.ShouldBe("Orders");
    }

    [Fact]
    public void MultipleInstances_ClearFromOneAffectsOther()
    {
        // Arrange
        var context1 = new ModuleExecutionContext();
        var context2 = new ModuleExecutionContext();
        context1.SetModule("Orders");

        // Act
        context2.ClearModule();

        // Assert
        context1.CurrentModule.ShouldBeNull();
        context2.CurrentModule.ShouldBeNull();
    }

    #endregion
}
