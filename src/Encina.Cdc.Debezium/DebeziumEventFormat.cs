namespace Encina.Cdc.Debezium;

/// <summary>
/// Specifies the format of events received from Debezium Server.
/// </summary>
public enum DebeziumEventFormat
{
    /// <summary>
    /// CloudEvents format (application/cloudevents+json).
    /// The default format for Debezium Server HTTP sink.
    /// </summary>
    CloudEvents = 0,

    /// <summary>
    /// Flat JSON format with Debezium envelope (before/after/source/op).
    /// </summary>
    Flat = 1
}
