using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using ReflectionAssembly = System.Reflection.Assembly;

namespace Encina.Testing.Architecture;

/// <summary>
/// A fluent builder for composing and executing multiple architecture rules.
/// </summary>
/// <remarks>
/// <para>
/// Use this builder when you want to apply multiple rules and get a combined result,
/// or when you need more flexibility than the predefined test base class provides.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = new EncinaArchitectureRulesBuilder(typeof(MyHandler).Assembly)
///     .EnforceHandlerAbstractions()
///     .EnforceSealedNotifications()
///     .EnforceSealedHandlers()
///     .EnforceLayerSeparation("MyApp.Domain", "MyApp.Application", "MyApp.Infrastructure")
///     .Verify();
/// </code>
/// </example>
public sealed class EncinaArchitectureRulesBuilder
{
    private readonly ArchUnitNET.Domain.Architecture _architecture;
    private readonly List<IArchRule> _rules = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaArchitectureRulesBuilder"/> class.
    /// </summary>
    /// <param name="assemblies">The assemblies to analyze.</param>
    /// <exception cref="ArgumentException">Thrown when no assemblies are provided.</exception>
    public EncinaArchitectureRulesBuilder(params ReflectionAssembly[] assemblies)
    {
        if (assemblies is null || assemblies.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be provided.", nameof(assemblies));
        }

        _architecture = new ArchLoader()
            .LoadAssemblies(assemblies)
            .Build();
    }

    /// <summary>
    /// Gets the architecture object for custom rule creation.
    /// </summary>
    public ArchUnitNET.Domain.Architecture Architecture => _architecture;

    /// <summary>
    /// Gets the number of rules currently registered.
    /// </summary>
    /// <remarks>Exposed for testing purposes only.</remarks>
    internal int RuleCount => _rules.Count;

    /// <summary>
    /// Adds a rule that handlers should not depend on infrastructure.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceHandlerAbstractions()
    {
        _rules.Add(EncinaArchitectureRules.HandlersShouldNotDependOnInfrastructure());
        return this;
    }

    /// <summary>
    /// Adds a rule that notifications should be sealed.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceSealedNotifications()
    {
        _rules.Add(EncinaArchitectureRules.NotificationsShouldBeSealed());
        return this;
    }

    /// <summary>
    /// Adds a rule that handlers should be sealed.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceSealedHandlers()
    {
        _rules.Add(EncinaArchitectureRules.HandlersShouldBeSealed());
        return this;
    }

    /// <summary>
    /// Adds a rule that behaviors should be sealed.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceSealedBehaviors()
    {
        _rules.Add(EncinaArchitectureRules.BehaviorsShouldBeSealed());
        return this;
    }

    /// <summary>
    /// Adds a rule that validators should follow naming conventions.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceValidatorNaming()
    {
        _rules.Add(EncinaArchitectureRules.ValidatorsShouldFollowNamingConvention());
        return this;
    }

    /// <summary>
    /// Adds a rule that domain should not depend on messaging.
    /// </summary>
    /// <param name="domainNamespace">The domain namespace pattern.</param>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceDomainMessagingIsolation(string domainNamespace)
    {
        _rules.Add(EncinaArchitectureRules.DomainShouldNotDependOnMessaging(domainNamespace));
        return this;
    }

    /// <summary>
    /// Adds rules for clean architecture layer separation.
    /// </summary>
    /// <param name="domainNamespace">The domain namespace pattern.</param>
    /// <param name="applicationNamespace">The application namespace pattern.</param>
    /// <param name="infrastructureNamespace">The infrastructure namespace pattern.</param>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceLayerSeparation(
        string domainNamespace,
        string applicationNamespace,
        string infrastructureNamespace)
    {
        _rules.Add(EncinaArchitectureRules.CleanArchitectureLayersShouldBeSeparated(
            domainNamespace,
            applicationNamespace,
            infrastructureNamespace));
        return this;
    }

    /// <summary>
    /// Adds a rule that repository interfaces should reside in domain.
    /// </summary>
    /// <param name="domainNamespace">The domain namespace pattern.</param>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceRepositoryInterfacesInDomain(string domainNamespace)
    {
        _rules.Add(EncinaArchitectureRules.RepositoryInterfacesShouldResideInDomain(domainNamespace));
        return this;
    }

    /// <summary>
    /// Adds a rule that repository implementations should reside in infrastructure.
    /// </summary>
    /// <param name="infrastructureNamespace">The infrastructure namespace pattern.</param>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceRepositoryImplementationsInInfrastructure(
        string infrastructureNamespace)
    {
        _rules.Add(EncinaArchitectureRules.RepositoryImplementationsShouldResideInInfrastructure(
            infrastructureNamespace));
        return this;
    }

    /// <summary>
    /// Adds a custom architecture rule.
    /// </summary>
    /// <param name="rule">The custom rule to add.</param>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder AddCustomRule(IArchRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        _rules.Add(rule);
        return this;
    }

    /// <summary>
    /// Adds a rule that request types should follow naming conventions.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceRequestNaming()
    {
        _rules.Add(EncinaArchitectureRules.RequestsShouldFollowNamingConvention());
        return this;
    }

    /// <summary>
    /// Adds a rule that aggregates should be sealed.
    /// </summary>
    /// <param name="aggregateNamespace">The namespace pattern for aggregates.</param>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceSealedAggregates(string aggregateNamespace)
    {
        _rules.Add(EncinaArchitectureRules.AggregatesShouldFollowPattern(aggregateNamespace));
        return this;
    }

    /// <summary>
    /// Adds a rule that value objects should be sealed.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceSealedValueObjects()
    {
        _rules.Add(EncinaArchitectureRules.ValueObjectsShouldBeSealed());
        return this;
    }

    /// <summary>
    /// Adds a rule that sagas should be sealed.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceSealedSagas()
    {
        _rules.Add(EncinaArchitectureRules.SagasShouldBeSealed());
        return this;
    }

    /// <summary>
    /// Adds a rule that event handlers should be sealed.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceSealedEventHandlers()
    {
        _rules.Add(EncinaArchitectureRules.EventHandlersShouldBeSealed());
        return this;
    }

    /// <summary>
    /// Applies all standard Encina architecture rules.
    /// </summary>
    /// <remarks>
    /// This does not include saga-specific rules. To enforce saga rules,
    /// call <see cref="ApplyAllSagaRules"/> explicitly.
    /// </remarks>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder ApplyAllStandardRules()
    {
        return EnforceHandlerAbstractions()
            .EnforceSealedNotifications()
            .EnforceSealedHandlers()
            .EnforceSealedBehaviors()
            .EnforceValidatorNaming()
            .EnforceSealedEventHandlers()
            .EnforceHandlerInterfaces()
            .EnforceCommandInterfaces()
            .EnforceQueryInterfaces()
            .EnforceHandlerControllerIsolation()
            .EnforcePipelineBehaviorInterfaces();
    }

    /// <summary>
    /// Applies all saga-specific architecture rules.
    /// </summary>
    /// <remarks>
    /// Saga rules are opt-in and not included in <see cref="ApplyAllStandardRules"/>.
    /// Call this method explicitly when your project uses sagas.
    /// </remarks>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder ApplyAllSagaRules()
    {
        return EnforceSealedSagas()
            .EnforceSealedSagaData();
    }

    #region CQRS Pattern Rules

    /// <summary>
    /// Adds a rule that handlers should implement the correct interface.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceHandlerInterfaces()
    {
        _rules.Add(EncinaArchitectureRules.HandlersShouldImplementCorrectInterface());
        return this;
    }

    /// <summary>
    /// Adds a rule that commands should implement ICommand.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceCommandInterfaces()
    {
        _rules.Add(EncinaArchitectureRules.CommandsShouldImplementICommand());
        return this;
    }

    /// <summary>
    /// Adds a rule that queries should implement IQuery.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceQueryInterfaces()
    {
        _rules.Add(EncinaArchitectureRules.QueriesShouldImplementIQuery());
        return this;
    }

    /// <summary>
    /// Adds a rule that handlers should not depend on controllers.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceHandlerControllerIsolation()
    {
        _rules.Add(EncinaArchitectureRules.HandlersShouldNotDependOnControllers());
        return this;
    }

    #endregion

    #region Pipeline Behavior Rules

    /// <summary>
    /// Adds a rule that pipeline behaviors should implement the correct interface.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforcePipelineBehaviorInterfaces()
    {
        _rules.Add(EncinaArchitectureRules.PipelineBehaviorsShouldImplementCorrectInterface());
        return this;
    }

    #endregion

    #region Saga Rules

    /// <summary>
    /// Adds a rule that saga data should be sealed.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public EncinaArchitectureRulesBuilder EnforceSealedSagaData()
    {
        _rules.Add(EncinaArchitectureRules.SagaDataShouldBeSealed());
        return this;
    }

    #endregion

    /// <summary>
    /// Verifies all added rules and throws if any violations are found.
    /// </summary>
    /// <exception cref="ArchitectureRuleException">Thrown when one or more rules are violated.</exception>
    public void Verify()
    {
        var violations = ExecuteVerification();

        if (violations.Count > 0)
        {
            throw new ArchitectureRuleException(violations);
        }
    }

    /// <summary>
    /// Verifies all added rules and returns a result without throwing.
    /// </summary>
    /// <returns>The verification result containing any violations.</returns>
    public ArchitectureVerificationResult VerifyWithResult()
    {
        var violations = ExecuteVerification();
        return new ArchitectureVerificationResult(violations);
    }

    private List<ArchitectureRuleViolation> ExecuteVerification()
    {
        var violations = new List<ArchitectureRuleViolation>();

        foreach (var rule in _rules)
        {
            try
            {
                rule.Check(_architecture);
            }
            catch (Exception ex)
            {
                violations.Add(new ArchitectureRuleViolation(rule.Description, ex.Message));
            }
        }

        return violations;
    }
}

/// <summary>
/// Represents a violation of an architecture rule.
/// </summary>
/// <param name="RuleName">The name of the violated rule.</param>
/// <param name="Message">The violation message.</param>
public sealed record ArchitectureRuleViolation(string RuleName, string Message);

/// <summary>
/// Represents the result of architecture verification.
/// </summary>
public sealed class ArchitectureVerificationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArchitectureVerificationResult"/> class.
    /// </summary>
    /// <param name="violations">The list of violations found.</param>
    public ArchitectureVerificationResult(IEnumerable<ArchitectureRuleViolation> violations)
    {
        Violations = violations.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets the violations found during verification.
    /// </summary>
    public IReadOnlyList<ArchitectureRuleViolation> Violations { get; }

    /// <summary>
    /// Gets a value indicating whether the verification passed.
    /// </summary>
    public bool IsSuccess => Violations.Count == 0;

    /// <summary>
    /// Gets a value indicating whether the verification failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;
}

/// <summary>
/// Exception thrown when architecture rules are violated.
/// </summary>
public sealed class ArchitectureRuleException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArchitectureRuleException"/> class.
    /// </summary>
    public ArchitectureRuleException()
        : base("Architecture verification failed.")
    {
        Violations = Array.Empty<ArchitectureRuleViolation>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchitectureRuleException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ArchitectureRuleException(string message)
        : base(message)
    {
        Violations = Array.Empty<ArchitectureRuleViolation>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchitectureRuleException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ArchitectureRuleException(string message, Exception innerException)
        : base(message, innerException)
    {
        Violations = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchitectureRuleException"/> class with a list of violations.
    /// </summary>
    /// <param name="violations">The list of violations.</param>
    public ArchitectureRuleException(IEnumerable<ArchitectureRuleViolation> violations)
        : base(FormatMessage(violations))
    {
        Violations = violations.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets the violations that caused this exception.
    /// </summary>
    public IReadOnlyList<ArchitectureRuleViolation> Violations { get; }

    private static string FormatMessage(IEnumerable<ArchitectureRuleViolation> violations)
    {
        var violationList = violations.ToList();
        var header = $"Architecture verification failed with {violationList.Count} violation(s):";
        var details = string.Join(Environment.NewLine, violationList.Select((v, i) =>
            $"  [{i + 1}] {v.RuleName}: {v.Message}"));
        return $"{header}{Environment.NewLine}{details}";
    }
}
