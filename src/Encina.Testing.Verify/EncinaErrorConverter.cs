using Argon;

namespace Encina.Testing.Verify;

/// <summary>
/// JSON converter for <see cref="EncinaError"/> that provides clean snapshot output.
/// </summary>
/// <remarks>
/// This converter excludes internal exception metadata and focuses on the
/// user-visible error information (message, code, details).
/// Verify uses Argon (a Newtonsoft.Json fork) for serialization.
/// </remarks>
internal sealed class EncinaErrorConverter : JsonConverter<EncinaError>
{
    /// <inheritdoc />
    public override EncinaError ReadJson(
        JsonReader reader,
        Type type,
        EncinaError existingValue,
        bool hasExisting,
        JsonSerializer serializer)
    {
        // We only need write support for snapshot testing
        throw new NotSupportedException("Reading EncinaError from JSON is not supported in snapshot testing.");
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, EncinaError value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Message");
        writer.WriteValue(value.Message);

        var code = value.GetCode();
        code.IfSome(c =>
        {
            writer.WritePropertyName("Code");
            writer.WriteValue(c);
        });

        var details = value.GetDetails();
        if (details.Count > 0)
        {
            writer.WritePropertyName("Details");
            serializer.Serialize(writer, details);
        }

        // Only include exception type, not the full exception
        value.Exception.IfSome(ex =>
        {
            writer.WritePropertyName("ExceptionType");
            writer.WriteValue(ex.GetType().Name);
        });

        writer.WriteEndObject();
    }
}
