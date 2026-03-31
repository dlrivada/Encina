using System.Reflection;
using Encina.EntityFrameworkCore.Scheduling;
using Shouldly;

namespace Encina.ContractTests.EntityFrameworkCore;

/// <summary>
/// Contract tests verifying that <see cref="IMessageScheduler"/> interface
/// defines the expected methods with correct signatures, return types, and generic constraints.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "Scheduling")]
public sealed class IMessageSchedulerContractTests
{
    private static readonly Type SchedulerType = typeof(IMessageScheduler);

    #region Interface Shape

    [Fact]
    public void IMessageScheduler_ShouldBeAnInterface()
    {
        SchedulerType.IsInterface.ShouldBeTrue(
            "IMessageScheduler should be an interface");
    }

    [Fact]
    public void IMessageScheduler_ShouldHaveExactlyFourMethods()
    {
        // Act
        var methods = SchedulerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        methods.Length.ShouldBe(4,
            "IMessageScheduler should have exactly 4 methods: ScheduleAsync (x2), ScheduleRecurringAsync, CancelAsync");
    }

    #endregion

    #region ScheduleAsync with DateTime

    [Fact]
    public void ScheduleAsync_WithDateTime_ShouldExist()
    {
        // Act - find the ScheduleAsync overload that takes DateTime
        var methods = SchedulerType.GetMethods()
            .Where(m => m.Name == "ScheduleAsync")
            .Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(DateTime)))
            .ToArray();

        // Assert
        methods.Length.ShouldBe(1,
            "IMessageScheduler should have exactly one ScheduleAsync overload with DateTime parameter");
    }

    [Fact]
    public void ScheduleAsync_WithDateTime_ShouldReturnTaskOfGuid()
    {
        // Act
        var method = SchedulerType.GetMethods()
            .Where(m => m.Name == "ScheduleAsync")
            .First(m => m.GetParameters().Any(p => p.ParameterType == typeof(DateTime)));

        // Assert
        method.ReturnType.ShouldBe(typeof(Task<Guid>),
            "ScheduleAsync(DateTime) should return Task<Guid>");
    }

    [Fact]
    public void ScheduleAsync_WithDateTime_ShouldHaveCorrectParameters()
    {
        // Act
        var method = SchedulerType.GetMethods()
            .Where(m => m.Name == "ScheduleAsync")
            .First(m => m.GetParameters().Any(p => p.ParameterType == typeof(DateTime)));

        var parameters = method.GetParameters();

        // Assert - 4 params: TMessage, DateTime, IRequestContext, CancellationToken
        parameters.Length.ShouldBe(4,
            "ScheduleAsync(DateTime) should have 4 parameters: TMessage, DateTime, IRequestContext, CancellationToken");

        parameters[0].ParameterType.IsGenericParameter.ShouldBeTrue(
            "First parameter should be the generic TMessage");
        parameters[1].ParameterType.ShouldBe(typeof(DateTime),
            "Second parameter should be DateTime");
        parameters[2].ParameterType.ShouldBe(typeof(IRequestContext),
            "Third parameter should be IRequestContext");
        parameters[3].ParameterType.ShouldBe(typeof(CancellationToken),
            "Fourth parameter should be CancellationToken");
    }

    [Fact]
    public void ScheduleAsync_WithDateTime_ShouldHaveNotnullConstraintOnTMessage()
    {
        // Act
        var method = SchedulerType.GetMethods()
            .Where(m => m.Name == "ScheduleAsync")
            .First(m => m.GetParameters().Any(p => p.ParameterType == typeof(DateTime)));

        var genericArgs = method.GetGenericArguments();

        // Assert
        genericArgs.Length.ShouldBe(1, "ScheduleAsync should have one generic type parameter");
        genericArgs[0].Name.ShouldBe("TMessage");
    }

    #endregion

    #region ScheduleAsync with TimeSpan

    [Fact]
    public void ScheduleAsync_WithTimeSpan_ShouldExist()
    {
        // Act
        var methods = SchedulerType.GetMethods()
            .Where(m => m.Name == "ScheduleAsync")
            .Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(TimeSpan)))
            .ToArray();

        // Assert
        methods.Length.ShouldBe(1,
            "IMessageScheduler should have exactly one ScheduleAsync overload with TimeSpan parameter");
    }

    [Fact]
    public void ScheduleAsync_WithTimeSpan_ShouldReturnTaskOfGuid()
    {
        // Act
        var method = SchedulerType.GetMethods()
            .Where(m => m.Name == "ScheduleAsync")
            .First(m => m.GetParameters().Any(p => p.ParameterType == typeof(TimeSpan)));

        // Assert
        method.ReturnType.ShouldBe(typeof(Task<Guid>),
            "ScheduleAsync(TimeSpan) should return Task<Guid>");
    }

    [Fact]
    public void ScheduleAsync_WithTimeSpan_ShouldHaveCorrectParameters()
    {
        // Act
        var method = SchedulerType.GetMethods()
            .Where(m => m.Name == "ScheduleAsync")
            .First(m => m.GetParameters().Any(p => p.ParameterType == typeof(TimeSpan)));

        var parameters = method.GetParameters();

        // Assert - 4 params: TMessage, TimeSpan, IRequestContext, CancellationToken
        parameters.Length.ShouldBe(4,
            "ScheduleAsync(TimeSpan) should have 4 parameters: TMessage, TimeSpan, IRequestContext, CancellationToken");

        parameters[0].ParameterType.IsGenericParameter.ShouldBeTrue(
            "First parameter should be the generic TMessage");
        parameters[1].ParameterType.ShouldBe(typeof(TimeSpan),
            "Second parameter should be TimeSpan");
        parameters[2].ParameterType.ShouldBe(typeof(IRequestContext),
            "Third parameter should be IRequestContext");
        parameters[3].ParameterType.ShouldBe(typeof(CancellationToken),
            "Fourth parameter should be CancellationToken");
    }

    #endregion

    #region ScheduleRecurringAsync

    [Fact]
    public void ScheduleRecurringAsync_ShouldExist()
    {
        // Act
        var methods = SchedulerType.GetMethods()
            .Where(m => m.Name == "ScheduleRecurringAsync")
            .ToArray();

        // Assert
        methods.Length.ShouldBe(1,
            "IMessageScheduler should have exactly one ScheduleRecurringAsync method");
    }

    [Fact]
    public void ScheduleRecurringAsync_ShouldReturnTaskOfGuid()
    {
        // Act
        var method = SchedulerType.GetMethods()
            .First(m => m.Name == "ScheduleRecurringAsync");

        // Assert
        method.ReturnType.ShouldBe(typeof(Task<Guid>),
            "ScheduleRecurringAsync should return Task<Guid>");
    }

    [Fact]
    public void ScheduleRecurringAsync_ShouldHaveCorrectParameters()
    {
        // Act
        var method = SchedulerType.GetMethods()
            .First(m => m.Name == "ScheduleRecurringAsync");

        var parameters = method.GetParameters();

        // Assert - 4 params: TMessage, string (cronExpression), IRequestContext, CancellationToken
        parameters.Length.ShouldBe(4,
            "ScheduleRecurringAsync should have 4 parameters: TMessage, string, IRequestContext, CancellationToken");

        parameters[0].ParameterType.IsGenericParameter.ShouldBeTrue(
            "First parameter should be the generic TMessage");
        parameters[1].ParameterType.ShouldBe(typeof(string),
            "Second parameter should be string (cronExpression)");
        parameters[2].ParameterType.ShouldBe(typeof(IRequestContext),
            "Third parameter should be IRequestContext");
        parameters[3].ParameterType.ShouldBe(typeof(CancellationToken),
            "Fourth parameter should be CancellationToken");
    }

    [Fact]
    public void ScheduleRecurringAsync_ShouldHaveGenericTMessageParameter()
    {
        // Act
        var method = SchedulerType.GetMethods()
            .First(m => m.Name == "ScheduleRecurringAsync");

        var genericArgs = method.GetGenericArguments();

        // Assert
        genericArgs.Length.ShouldBe(1, "ScheduleRecurringAsync should have one generic type parameter");
        genericArgs[0].Name.ShouldBe("TMessage");
    }

    #endregion

    #region CancelAsync

    [Fact]
    public void CancelAsync_ShouldExist()
    {
        // Act
        var methods = SchedulerType.GetMethods()
            .Where(m => m.Name == "CancelAsync")
            .ToArray();

        // Assert
        methods.Length.ShouldBe(1,
            "IMessageScheduler should have exactly one CancelAsync method");
    }

    [Fact]
    public void CancelAsync_ShouldReturnTaskOfBool()
    {
        // Act
        var method = SchedulerType.GetMethods()
            .First(m => m.Name == "CancelAsync");

        // Assert
        method.ReturnType.ShouldBe(typeof(Task<bool>),
            "CancelAsync should return Task<bool>");
    }

    [Fact]
    public void CancelAsync_ShouldHaveCorrectParameters()
    {
        // Act
        var method = SchedulerType.GetMethods()
            .First(m => m.Name == "CancelAsync");

        var parameters = method.GetParameters();

        // Assert - 2 params: Guid messageId, CancellationToken
        parameters.Length.ShouldBe(2,
            "CancelAsync should have 2 parameters: Guid, CancellationToken");

        parameters[0].ParameterType.ShouldBe(typeof(Guid),
            "First parameter should be Guid (messageId)");
        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken),
            "Second parameter should be CancellationToken");
    }

    [Fact]
    public void CancelAsync_ShouldNotBeGeneric()
    {
        // Act
        var method = SchedulerType.GetMethods()
            .First(m => m.Name == "CancelAsync");

        // Assert
        method.IsGenericMethod.ShouldBeFalse(
            "CancelAsync should not be a generic method");
    }

    #endregion

    #region Method Name Consistency

    [Theory]
    [InlineData("ScheduleAsync", 2)]
    [InlineData("ScheduleRecurringAsync", 1)]
    [InlineData("CancelAsync", 1)]
    public void IMessageScheduler_ShouldHaveExpectedMethodCounts(string methodName, int expectedCount)
    {
        // Act
        var count = SchedulerType.GetMethods()
            .Count(m => m.Name == methodName);

        // Assert
        count.ShouldBe(expectedCount,
            $"IMessageScheduler should have {expectedCount} method(s) named {methodName}");
    }

    #endregion
}
