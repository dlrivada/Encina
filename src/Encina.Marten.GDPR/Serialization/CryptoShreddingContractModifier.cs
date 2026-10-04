using System.Text.Json.Serialization.Metadata;

using Encina.Marten.GDPR.Diagnostics;

using Microsoft.Extensions.Logging;

namespace Encina.Marten.GDPR;

/// <summary>
/// The System.Text.Json contract modifier that installs crypto-shredding: it wraps <c>JsonPropertyInfo.Get</c> of
/// every crypto field (encryption) and chains <c>JsonTypeInfo.OnDeserialized</c> (decryption).
/// </summary>
/// <remarks>
/// <para>
/// Every contract is classified, whatever its <see cref="JsonTypeInfoKind"/>. A type with any problem throws
/// <see cref="CryptoShreddingConfigurationException"/>; System.Text.Json caches that failure for the type, so it
/// stays rejected for the life of the options and nothing of it is ever written in plaintext.
/// </para>
/// <para>
/// Types without crypto fields keep their contracts untouched.
/// </para>
/// </remarks>
internal sealed class CryptoShreddingContractModifier
{
    private readonly CryptoShreddingEngine _engine;
    private readonly ILogger _logger;

    internal CryptoShreddingContractModifier(CryptoShreddingEngine engine, ILogger logger)
    {
        _engine = engine;
        _logger = logger;
    }

    /// <summary>The modifier entry point (<c>WithAddedModifier(modifier.Modify)</c>).</summary>
    internal void Modify(JsonTypeInfo typeInfo)
    {
        _engine.Registry.MarkSeen(typeInfo.Type);

        var shape = CryptoShreddedPropertyClassifier.GetShape(typeInfo.Type);
        if (!shape.IsRelevant && !CryptoShreddedPropertyClassifier.ReachesCryptoOwner(typeInfo.Type))
        {
            return;
        }

        var issues = CryptoShreddedContractRules.Classify(typeInfo, shape);
        if (issues.Count > 0)
        {
            Reject(typeInfo.Type, issues);
        }

        if (!shape.Fields.IsEmpty)
        {
            Install(typeInfo, shape);
        }
    }

    private void Reject(Type type, IReadOnlyList<CryptoShreddedPropertyIssue> issues)
    {
        foreach (var issue in issues)
        {
            _logger.AttributeMisconfigured(issue.DeclaringTypeName, issue.PropertyName, issue.Problems.ToString());
        }

        _engine.Registry.MarkRejected(type);
        CryptoShreddingDiagnostics.MisconfiguredTotal.Add(1);
        throw new CryptoShreddingConfigurationException(CryptoShreddingConfigurationProblem.MisconfiguredProperties, issues);
    }

    private void Install(JsonTypeInfo typeInfo, CryptoShreddedTypeShape shape)
    {
        var plan = new CryptoShreddingTypePlan(typeInfo.Type, shape.Fields);
        foreach (var field in shape.Fields)
        {
            WrapGetter(typeInfo, field);
        }

        var previous = typeInfo.OnDeserialized;
        var engine = _engine;
        typeInfo.OnDeserialized = owner =>
        {
            previous?.Invoke(owner);
            engine.OnOwnerDeserialized(owner, plan);
        };

        _engine.Registry.Register(plan);
        _logger.CryptoContractBuilt(typeInfo.Type.Name, shape.Fields.Length);
    }

    private void WrapGetter(JsonTypeInfo typeInfo, CryptoShreddedField field)
    {
        // The classifier proved the property is in the contract with a getter (otherwise it is an issue).
        var property = typeInfo.Properties.First(p => p.AttributeProvider is System.Reflection.PropertyInfo info && info.Name == field.Name);
        var original = property.Get!;
        var engine = _engine;
        property.Get = owner => engine.EncryptForWrite(owner, field, (string?)original(owner));
    }
}
