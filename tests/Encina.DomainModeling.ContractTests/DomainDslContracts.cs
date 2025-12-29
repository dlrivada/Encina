using Encina.DomainModeling;
using System.Reflection;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests verifying the public API of domain DSL patterns.
/// </summary>
public class DomainDslContracts
{
    // === DomainBuilder<T, TBuilder> Class ===

    [Fact]
    public void DomainBuilder_IsAbstractGenericClass()
    {
        Assert.True(typeof(DomainBuilder<,>).IsAbstract);
        Assert.True(typeof(DomainBuilder<,>).IsClass);
        Assert.True(typeof(DomainBuilder<,>).IsGenericType);
        Assert.Equal(2, typeof(DomainBuilder<,>).GetGenericArguments().Length);
    }

    [Fact]
    public void DomainBuilder_HasBuildMethod()
    {
        var method = typeof(DomainBuilder<,>).GetMethod("Build");
        Assert.NotNull(method);
        Assert.True(method.IsAbstract);
    }

    [Fact]
    public void DomainBuilder_HasBuildOrThrowMethod()
    {
        var method = typeof(DomainBuilder<,>).GetMethod("BuildOrThrow");
        Assert.NotNull(method);
    }

    [Fact]
    public void DomainBuilder_HasTryBuildMethod()
    {
        var method = typeof(DomainBuilder<,>).GetMethod("TryBuild");
        Assert.NotNull(method);
    }

    // === AggregateBuilder<TAggregate, TId, TBuilder> Class ===

    [Fact]
    public void AggregateBuilder_IsAbstractGenericClass()
    {
        Assert.True(typeof(AggregateBuilder<,,>).IsAbstract);
        Assert.True(typeof(AggregateBuilder<,,>).IsClass);
        Assert.True(typeof(AggregateBuilder<,,>).IsGenericType);
        Assert.Equal(3, typeof(AggregateBuilder<,,>).GetGenericArguments().Length);
    }

    [Fact]
    public void AggregateBuilder_ExtendsDomainBuilder()
    {
        var baseType = typeof(AggregateBuilder<,,>).BaseType;
        Assert.NotNull(baseType);
        Assert.True(baseType.IsGenericType);
        Assert.Equal(typeof(DomainBuilder<,>), baseType.GetGenericTypeDefinition());
    }

    [Fact]
    public void AggregateBuilder_HasAddRuleMethod()
    {
        var method = typeof(AggregateBuilder<,,>).GetMethod("AddRule", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        Assert.True(method.IsFamily); // protected
    }

    [Fact]
    public void AggregateBuilder_HasCreateAggregateMethod()
    {
        var method = typeof(AggregateBuilder<,,>).GetMethod("CreateAggregate", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        Assert.True(method.IsAbstract);
        Assert.True(method.IsFamily); // protected
    }

    // === DomainBuilderError Record ===

    [Fact]
    public void DomainBuilderError_IsRecord()
    {
        Assert.True(typeof(DomainBuilderError).IsClass);
        var equalityContract = typeof(DomainBuilderError).GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(equalityContract);
    }

    [Fact]
    public void DomainBuilderError_HasRequiredProperties()
    {
        var properties = typeof(DomainBuilderError).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.Contains(properties, p => p.Name == "Message");
        Assert.Contains(properties, p => p.Name == "ErrorCode");
        Assert.Contains(properties, p => p.Name == "Details");
    }

    [Fact]
    public void DomainBuilderError_HasMissingValueFactory()
    {
        var method = typeof(DomainBuilderError).GetMethod("MissingValue");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void DomainBuilderError_HasValidationFailedFactory()
    {
        var method = typeof(DomainBuilderError).GetMethod("ValidationFailed");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void DomainBuilderError_HasBusinessRulesViolatedFactory()
    {
        var method = typeof(DomainBuilderError).GetMethod("BusinessRulesViolated");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void DomainBuilderError_HasInvalidStateFactory()
    {
        var method = typeof(DomainBuilderError).GetMethod("InvalidState");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    // === DomainDslExtensions ===

    [Fact]
    public void DomainDslExtensions_IsStaticClass()
    {
        Assert.True(typeof(DomainDslExtensions).IsAbstract);
        Assert.True(typeof(DomainDslExtensions).IsSealed);
    }

    [Fact]
    public void DomainDslExtensions_HasIsMethod()
    {
        var method = typeof(DomainDslExtensions).GetMethod("Is");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void DomainDslExtensions_HasSatisfiesMethod()
    {
        var method = typeof(DomainDslExtensions).GetMethod("Satisfies");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void DomainDslExtensions_HasViolatesMethod()
    {
        var method = typeof(DomainDslExtensions).GetMethod("Violates");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void DomainDslExtensions_HasPassesMethod()
    {
        var method = typeof(DomainDslExtensions).GetMethod("Passes");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void DomainDslExtensions_HasFailsMethod()
    {
        var method = typeof(DomainDslExtensions).GetMethod("Fails");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void DomainDslExtensions_HasEnsureValidMethod()
    {
        var method = typeof(DomainDslExtensions).GetMethod("EnsureValid");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void DomainDslExtensions_HasEnsureNotNullMethod()
    {
        var method = typeof(DomainDslExtensions).GetMethod("EnsureNotNull");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    // === Quantity Struct ===

    [Fact]
    public void Quantity_IsValueType()
    {
        Assert.True(typeof(Quantity).IsValueType);
    }

    [Fact]
    public void Quantity_ImplementsIEquatable()
    {
        Assert.True(typeof(IEquatable<Quantity>).IsAssignableFrom(typeof(Quantity)));
    }

    [Fact]
    public void Quantity_ImplementsIComparable()
    {
        Assert.True(typeof(IComparable<Quantity>).IsAssignableFrom(typeof(Quantity)));
    }

    [Fact]
    public void Quantity_HasValueProperty()
    {
        var property = typeof(Quantity).GetProperty("Value");
        Assert.NotNull(property);
        Assert.Equal(typeof(int), property.PropertyType);
    }

    [Fact]
    public void Quantity_HasCreateMethod()
    {
        var method = typeof(Quantity).GetMethod("Create");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void Quantity_HasFromMethod()
    {
        var method = typeof(Quantity).GetMethod("From");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void Quantity_HasZeroProperty()
    {
        var property = typeof(Quantity).GetProperty("Zero");
        Assert.NotNull(property);
    }

    [Fact]
    public void Quantity_HasOneProperty()
    {
        var property = typeof(Quantity).GetProperty("One");
        Assert.NotNull(property);
    }

    [Fact]
    public void Quantity_HasArithmeticMethods()
    {
        Assert.NotNull(typeof(Quantity).GetMethod("Add"));
        Assert.NotNull(typeof(Quantity).GetMethod("Subtract"));
        Assert.NotNull(typeof(Quantity).GetMethod("Multiply"));
    }

    // === Percentage Struct ===

    [Fact]
    public void Percentage_IsValueType()
    {
        Assert.True(typeof(Percentage).IsValueType);
    }

    [Fact]
    public void Percentage_ImplementsIEquatable()
    {
        Assert.True(typeof(IEquatable<Percentage>).IsAssignableFrom(typeof(Percentage)));
    }

    [Fact]
    public void Percentage_ImplementsIComparable()
    {
        Assert.True(typeof(IComparable<Percentage>).IsAssignableFrom(typeof(Percentage)));
    }

    [Fact]
    public void Percentage_HasValueProperty()
    {
        var property = typeof(Percentage).GetProperty("Value");
        Assert.NotNull(property);
        Assert.Equal(typeof(decimal), property.PropertyType);
    }

    [Fact]
    public void Percentage_HasCreateMethod()
    {
        var method = typeof(Percentage).GetMethod("Create");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void Percentage_HasFromMethod()
    {
        var method = typeof(Percentage).GetMethod("From");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void Percentage_HasZeroProperty()
    {
        var property = typeof(Percentage).GetProperty("Zero");
        Assert.NotNull(property);
    }

    [Fact]
    public void Percentage_HasFullProperty()
    {
        var property = typeof(Percentage).GetProperty("Full");
        Assert.NotNull(property);
    }

    [Fact]
    public void Percentage_HasApplyToMethod()
    {
        var method = typeof(Percentage).GetMethod("ApplyTo");
        Assert.NotNull(method);
    }

    [Fact]
    public void Percentage_HasAsFractionProperty()
    {
        var property = typeof(Percentage).GetProperty("AsFraction");
        Assert.NotNull(property);
    }

    [Fact]
    public void Percentage_HasComplementProperty()
    {
        var property = typeof(Percentage).GetProperty("Complement");
        Assert.NotNull(property);
    }

    // === DateRange Struct ===

    [Fact]
    public void DateRange_IsValueType()
    {
        Assert.True(typeof(DateRange).IsValueType);
    }

    [Fact]
    public void DateRange_ImplementsIEquatable()
    {
        Assert.True(typeof(IEquatable<DateRange>).IsAssignableFrom(typeof(DateRange)));
    }

    [Fact]
    public void DateRange_HasStartProperty()
    {
        var property = typeof(DateRange).GetProperty("Start");
        Assert.NotNull(property);
        Assert.Equal(typeof(DateOnly), property.PropertyType);
    }

    [Fact]
    public void DateRange_HasEndProperty()
    {
        var property = typeof(DateRange).GetProperty("End");
        Assert.NotNull(property);
        Assert.Equal(typeof(DateOnly), property.PropertyType);
    }

    [Fact]
    public void DateRange_HasCreateMethod()
    {
        var method = typeof(DateRange).GetMethod("Create");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void DateRange_HasFromMethod()
    {
        var method = typeof(DateRange).GetMethod("From");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void DateRange_HasSingleDayMethod()
    {
        var method = typeof(DateRange).GetMethod("SingleDay");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void DateRange_HasDaysMethod()
    {
        var method = typeof(DateRange).GetMethod("Days");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void DateRange_HasTotalDaysProperty()
    {
        var property = typeof(DateRange).GetProperty("TotalDays");
        Assert.NotNull(property);
    }

    [Fact]
    public void DateRange_HasContainsMethod()
    {
        var method = typeof(DateRange).GetMethod("Contains");
        Assert.NotNull(method);
    }

    [Fact]
    public void DateRange_HasOverlapsMethod()
    {
        var method = typeof(DateRange).GetMethod("Overlaps");
        Assert.NotNull(method);
    }

    [Fact]
    public void DateRange_HasFullyContainsMethod()
    {
        var method = typeof(DateRange).GetMethod("FullyContains");
        Assert.NotNull(method);
    }

    [Fact]
    public void DateRange_HasIntersectMethod()
    {
        var method = typeof(DateRange).GetMethod("Intersect");
        Assert.NotNull(method);
    }

    [Fact]
    public void DateRange_HasExtendByMethod()
    {
        var method = typeof(DateRange).GetMethod("ExtendBy");
        Assert.NotNull(method);
    }

    // === TimeRange Struct ===

    [Fact]
    public void TimeRange_IsValueType()
    {
        Assert.True(typeof(TimeRange).IsValueType);
    }

    [Fact]
    public void TimeRange_ImplementsIEquatable()
    {
        Assert.True(typeof(IEquatable<TimeRange>).IsAssignableFrom(typeof(TimeRange)));
    }

    [Fact]
    public void TimeRange_HasStartProperty()
    {
        var property = typeof(TimeRange).GetProperty("Start");
        Assert.NotNull(property);
        Assert.Equal(typeof(TimeOnly), property.PropertyType);
    }

    [Fact]
    public void TimeRange_HasEndProperty()
    {
        var property = typeof(TimeRange).GetProperty("End");
        Assert.NotNull(property);
        Assert.Equal(typeof(TimeOnly), property.PropertyType);
    }

    [Fact]
    public void TimeRange_HasCreateMethod()
    {
        var method = typeof(TimeRange).GetMethod("Create");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void TimeRange_HasFromMethod()
    {
        var method = typeof(TimeRange).GetMethod("From");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void TimeRange_HasDurationProperty()
    {
        var property = typeof(TimeRange).GetProperty("Duration");
        Assert.NotNull(property);
        Assert.Equal(typeof(TimeSpan), property.PropertyType);
    }

    [Fact]
    public void TimeRange_HasContainsMethod()
    {
        var method = typeof(TimeRange).GetMethod("Contains");
        Assert.NotNull(method);
    }

    [Fact]
    public void TimeRange_HasOverlapsMethod()
    {
        var method = typeof(TimeRange).GetMethod("Overlaps");
        Assert.NotNull(method);
    }
}
