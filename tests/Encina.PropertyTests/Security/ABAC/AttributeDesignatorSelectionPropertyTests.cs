using Encina.Security.ABAC;
using Encina.Security.ABAC.Evaluation;

using FsCheck.Xunit;

namespace Encina.PropertyTests.Security.ABAC;

/// <summary>
/// Property-based tests for attribute designator selection (#1983): for random attribute
/// dictionaries, a designator's result is exactly the values stored under its attribute identifier
/// whose data type equals the designator's, in stored order; values of other attributes and of other
/// data types never appear.
/// </summary>
public sealed class AttributeDesignatorSelectionPropertyTests
{
    private static readonly string[] AttributeIds = ["department", "subject-id", "identity-kind", "role", "level", "region"];

    private static readonly string[] DataTypes = [XACMLDataTypes.String, XACMLDataTypes.Integer, XACMLDataTypes.Boolean];

    private static readonly AttributeCategory[] Categories =
        [AttributeCategory.Subject, AttributeCategory.Resource, AttributeCategory.Environment, AttributeCategory.Action];

    private readonly ConditionEvaluator _evaluator = new(new DefaultFunctionRegistry());

    [Property(MaxTest = 500)]
    public bool Designator_ReturnsExactlyTheValuesOfItsAttributeIdAndDataType(
        (byte Id, byte Type, byte Category)[] entries,
        byte designatorId,
        byte designatorType,
        byte designatorCategory)
    {
        // Each entry stores a unique value (its index) under a random category, attribute id and data type.
        var stored = (entries ?? [])
            .Select((entry, index) => (
                Category: Categories[entry.Category % Categories.Length],
                Id: AttributeIds[entry.Id % AttributeIds.Length],
                Value: new AttributeValue { DataType = DataTypes[entry.Type % DataTypes.Length], Value = index }))
            .ToList();

        var context = new PolicyEvaluationContext
        {
            SubjectAttributes = BagsOf(stored, AttributeCategory.Subject),
            ResourceAttributes = BagsOf(stored, AttributeCategory.Resource),
            EnvironmentAttributes = BagsOf(stored, AttributeCategory.Environment),
            ActionAttributes = BagsOf(stored, AttributeCategory.Action),
            RequestType = typeof(AttributeDesignatorSelectionPropertyTests)
        };

        var designator = new AttributeDesignator
        {
            Category = Categories[designatorCategory % Categories.Length],
            AttributeId = AttributeIds[designatorId % AttributeIds.Length],
            DataType = DataTypes[designatorType % DataTypes.Length]
        };

        var expected = stored
            .Where(s => s.Category == designator.Category && s.Id == designator.AttributeId && s.Value.DataType == designator.DataType)
            .Select(s => s.Value.Value)
            .ToList();

        var actual = _evaluator.Evaluate(designator, context).Match(Right: ValuesOf, Left: _ => null);

        return actual is not null && actual.SequenceEqual(expected);
    }

    [Property(MaxTest = 300)]
    public bool BuiltContext_DesignatorReadsOnlyTheValueStoredUnderItsKey(int[] values, byte designatorId)
    {
        // Distinct keys from the pool; each holds a distinct integer or its string form.
        var subject = new Dictionary<string, object>();
        for (var index = 0; index < (values ?? []).Length && index < AttributeIds.Length; index++)
        {
            subject[AttributeIds[index]] = index % 2 == 0 ? values![index] : values![index].ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var context = AttributeContextBuilder.Build(
            subject, new Dictionary<string, object>(), new Dictionary<string, object>(), typeof(AttributeDesignatorSelectionPropertyTests));
        var attributeId = AttributeIds[designatorId % AttributeIds.Length];

        // One designator per data type: only the one matching the stored value's type finds it.
        foreach (var dataType in new[] { XACMLDataTypes.Integer, XACMLDataTypes.String })
        {
            var designator = new AttributeDesignator
            {
                Category = AttributeCategory.Subject,
                AttributeId = attributeId,
                DataType = dataType
            };

            var expected = subject.TryGetValue(attributeId, out var value)
                && (value is int ? XACMLDataTypes.Integer : XACMLDataTypes.String) == dataType
                    ? new List<object?> { value }
                    : [];

            var actual = _evaluator.Evaluate(designator, context).Match(Right: ValuesOf, Left: _ => null);
            if (actual is null || !actual.SequenceEqual(expected))
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<string, AttributeBag> BagsOf(
        List<(AttributeCategory Category, string Id, AttributeValue Value)> stored,
        AttributeCategory category) =>
        stored
            .Where(s => s.Category == category)
            .GroupBy(s => s.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => AttributeBag.FromValues(g.Select(s => s.Value).ToList()), StringComparer.Ordinal);

    // The designator unwraps a single value and returns the bag otherwise (empty or multi-valued).
    private static List<object?> ValuesOf(object? result) => result switch
    {
        AttributeBag bag => bag.Values.Select(v => v.Value).ToList(),
        _ => [result]
    };
}
