using System.Text.Json;
using System.Text.Json.Serialization;

namespace Encina.Security.ABAC.Persistence;

/// <summary>
/// Custom <see cref="JsonConverter{T}"/> for the polymorphic <see cref="IExpression"/> hierarchy.
/// </summary>
/// <remarks>
/// <para>
/// Uses a <c>$type</c> discriminator property to distinguish between the four
/// <see cref="IExpression"/> implementations:
/// </para>
/// <list type="bullet">
/// <item><description><c>"apply"</c> — <see cref="Apply"/> (function application with recursive arguments)</description></item>
/// <item><description><c>"designator"</c> — <see cref="AttributeDesignator"/> (attribute lookup by category)</description></item>
/// <item><description><c>"value"</c> — <see cref="AttributeValue"/> (typed literal value)</description></item>
/// <item><description><c>"reference"</c> — <see cref="VariableReference"/> (reference to a named variable)</description></item>
/// </list>
/// <para>
/// The converter handles recursive expression trees: an <see cref="Apply"/> node contains
/// <see cref="IExpression"/> arguments, which can themselves be nested <see cref="Apply"/> nodes.
/// </para>
/// </remarks>
public sealed class ExpressionJsonConverter : JsonConverter<IExpression>
{
    /// <summary>Discriminator value for <see cref="Apply"/>.</summary>
    internal const string TypeApply = "apply";

    /// <summary>Discriminator value for <see cref="AttributeDesignator"/>.</summary>
    internal const string TypeDesignator = "designator";

    /// <summary>Discriminator value for <see cref="AttributeValue"/>.</summary>
    internal const string TypeValue = "value";

    /// <summary>Discriminator value for <see cref="VariableReference"/>.</summary>
    internal const string TypeReference = "reference";

    private const string TypePropertyName = "$type";

    /// <inheritdoc />
    public override IExpression? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token for IExpression.");
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (!root.TryGetProperty(TypePropertyName, out var typeProperty))
        {
            throw new JsonException($"Missing '{TypePropertyName}' discriminator property on IExpression.");
        }

        var discriminator = typeProperty.GetString();

        return discriminator switch
        {
            TypeApply => DeserializeApply(root, options),
            TypeDesignator => DeserializeAttributeDesignator(root),
            TypeValue => DeserializeAttributeValue(root),
            TypeReference => DeserializeVariableReference(root),
            _ => throw new JsonException($"Unknown IExpression type discriminator: '{discriminator}'.")
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IExpression value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        switch (value)
        {
            case Apply apply:
                WriteApply(writer, apply, options);
                break;
            case AttributeDesignator designator:
                WriteAttributeDesignator(writer, designator);
                break;
            case AttributeValue attributeValue:
                WriteAttributeValue(writer, attributeValue);
                break;
            case VariableReference reference:
                WriteVariableReference(writer, reference);
                break;
            default:
                throw new JsonException($"Unknown IExpression implementation: '{value.GetType().Name}'.");
        }
    }

    // ── Apply ────────────────────────────────────────────────────────

    private static Apply DeserializeApply(JsonElement root, JsonSerializerOptions options)
    {
        var functionId = root.GetProperty("functionId").GetString()
            ?? throw new JsonException("Apply.functionId is required.");

        var arguments = new List<IExpression>();
        if (root.TryGetProperty("arguments", out var argsElement) && argsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var argElement in argsElement.EnumerateArray())
            {
                var raw = argElement.GetRawText();
                var expression = JsonSerializer.Deserialize<IExpression>(raw, options)
                    ?? throw new JsonException("Failed to deserialize IExpression argument.");
                arguments.Add(expression);
            }
        }

        return new Apply
        {
            FunctionId = functionId,
            Arguments = arguments
        };
    }

    private static void WriteApply(Utf8JsonWriter writer, Apply apply, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(TypePropertyName, TypeApply);
        writer.WriteString("functionId", apply.FunctionId);

        writer.WritePropertyName("arguments");
        writer.WriteStartArray();
        foreach (var argument in apply.Arguments)
        {
            JsonSerializer.Serialize(writer, argument, options);
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    // ── AttributeDesignator ──────────────────────────────────────────

    private static AttributeDesignator DeserializeAttributeDesignator(JsonElement root)
    {
        var categoryStr = root.GetProperty("category").GetString()
            ?? throw new JsonException("AttributeDesignator.category is required.");

        if (!Enum.TryParse<AttributeCategory>(categoryStr, ignoreCase: true, out var category))
        {
            throw new JsonException($"Unknown AttributeCategory: '{categoryStr}'.");
        }

        var attributeId = root.GetProperty("attributeId").GetString()
            ?? throw new JsonException("AttributeDesignator.attributeId is required.");

        var dataType = root.GetProperty("dataType").GetString()
            ?? throw new JsonException("AttributeDesignator.dataType is required.");

        var mustBePresent = root.TryGetProperty("mustBePresent", out var mbpElement)
            && mbpElement.GetBoolean();

        return new AttributeDesignator
        {
            Category = category,
            AttributeId = attributeId,
            DataType = dataType,
            MustBePresent = mustBePresent
        };
    }

    private static void WriteAttributeDesignator(Utf8JsonWriter writer, AttributeDesignator designator)
    {
        writer.WriteStartObject();
        writer.WriteString(TypePropertyName, TypeDesignator);
        writer.WriteString("category", designator.Category.ToString());
        writer.WriteString("attributeId", designator.AttributeId);
        writer.WriteString("dataType", designator.DataType);
        writer.WriteBoolean("mustBePresent", designator.MustBePresent);
        writer.WriteEndObject();
    }

    // ── AttributeValue ───────────────────────────────────────────────

    private static AttributeValue DeserializeAttributeValue(JsonElement root)
    {
        var dataType = root.GetProperty("dataType").GetString()
            ?? throw new JsonException("AttributeValue.dataType is required.");

        object? value = null;
        if (root.TryGetProperty("value", out var valueElement))
        {
            value = ConvertJsonElementToObject(valueElement);
        }

        return new AttributeValue
        {
            DataType = dataType,
            Value = value
        };
    }

    private static void WriteAttributeValue(Utf8JsonWriter writer, AttributeValue attributeValue)
    {
        writer.WriteStartObject();
        writer.WriteString(TypePropertyName, TypeValue);
        writer.WriteString("dataType", attributeValue.DataType);

        writer.WritePropertyName("value");
        WriteObjectValue(writer, attributeValue.Value);

        writer.WriteEndObject();
    }

    // ── VariableReference ────────────────────────────────────────────

    private static VariableReference DeserializeVariableReference(JsonElement root)
    {
        var variableId = root.GetProperty("variableId").GetString()
            ?? throw new JsonException("VariableReference.variableId is required.");

        return new VariableReference
        {
            VariableId = variableId
        };
    }

    private static void WriteVariableReference(Utf8JsonWriter writer, VariableReference reference)
    {
        writer.WriteStartObject();
        writer.WriteString(TypePropertyName, TypeReference);
        writer.WriteString("variableId", reference.VariableId);
        writer.WriteEndObject();
    }

    // ── Value Helpers ────────────────────────────────────────────────

    /// <summary>
    /// Converts a <see cref="JsonElement"/> to a primitive CLR object for <see cref="AttributeValue.Value"/>.
    /// </summary>
    internal static object? ConvertJsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            // For arrays and objects, preserve as raw JSON string so round-trip is lossless
            JsonValueKind.Array or JsonValueKind.Object => element.GetRawText(),
            _ => element.GetRawText()
        };
    }

    /// <summary>
    /// Writes a boxed <see cref="object"/> value to a <see cref="Utf8JsonWriter"/>.
    /// </summary>
    private static void WriteObjectValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case float f:
                writer.WriteNumberValue(f);
                break;
            case decimal dec:
                writer.WriteNumberValue(dec);
                break;
            case DateTime dt:
                writer.WriteStringValue(dt.ToString("O"));
                break;
            case DateTimeOffset dto:
                writer.WriteStringValue(dto.ToString("O"));
                break;
            case DateOnly dateOnly:
                writer.WriteStringValue(dateOnly.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
                break;
            case TimeOnly timeOnly:
                writer.WriteStringValue(timeOnly.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
                break;
            default:
                // Fallback: serialize as string to preserve round-trip
                writer.WriteStringValue(value.ToString());
                break;
        }
    }
}
