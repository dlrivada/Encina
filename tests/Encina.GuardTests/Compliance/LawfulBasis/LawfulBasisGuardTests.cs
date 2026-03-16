using System.Reflection;
using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis;
using Encina.Compliance.LawfulBasis.Abstractions;
using Encina.Compliance.LawfulBasis.AutoRegistration;
using Encina.Compliance.LawfulBasis.Health;
using Encina.Compliance.LawfulBasis.ReadModels;
using Encina.Compliance.LawfulBasis.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using GDPRLawfulBasis = Encina.Compliance.GDPR.LawfulBasis;

namespace Encina.GuardTests.Compliance.LawfulBasis;

/// <summary>
/// Guard tests for lawful basis module classes to verify null parameter handling in constructors.
/// </summary>
public class LawfulBasisGuardTests
{
    #region DefaultLawfulBasisService Guards

    private readonly IAggregateRepository<global::Encina.Compliance.LawfulBasis.Aggregates.LawfulBasisAggregate> _registrationRepository =
        Substitute.For<IAggregateRepository<global::Encina.Compliance.LawfulBasis.Aggregates.LawfulBasisAggregate>>();

    private readonly IAggregateRepository<global::Encina.Compliance.LawfulBasis.Aggregates.LIAAggregate> _liaRepository =
        Substitute.For<IAggregateRepository<global::Encina.Compliance.LawfulBasis.Aggregates.LIAAggregate>>();

    private readonly IReadModelRepository<LawfulBasisReadModel> _registrationReadModels =
        Substitute.For<IReadModelRepository<LawfulBasisReadModel>>();

    private readonly IReadModelRepository<LIAReadModel> _liaReadModels =
        Substitute.For<IReadModelRepository<LIAReadModel>>();

    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<DefaultLawfulBasisService> _serviceLogger = NullLogger<DefaultLawfulBasisService>.Instance;

    [Fact]
    public void DefaultLawfulBasisService_NullRegistrationRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLawfulBasisService(
            null!, _liaRepository, _registrationReadModels, _liaReadModels,
            _cache, _timeProvider, _serviceLogger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("registrationRepository");
    }

    [Fact]
    public void DefaultLawfulBasisService_NullLIARepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLawfulBasisService(
            _registrationRepository, null!, _registrationReadModels, _liaReadModels,
            _cache, _timeProvider, _serviceLogger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("liaRepository");
    }

    [Fact]
    public void DefaultLawfulBasisService_NullRegistrationReadModels_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLawfulBasisService(
            _registrationRepository, _liaRepository, null!, _liaReadModels,
            _cache, _timeProvider, _serviceLogger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("registrationReadModels");
    }

    [Fact]
    public void DefaultLawfulBasisService_NullLIAReadModels_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLawfulBasisService(
            _registrationRepository, _liaRepository, _registrationReadModels, null!,
            _cache, _timeProvider, _serviceLogger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("liaReadModels");
    }

    [Fact]
    public void DefaultLawfulBasisService_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLawfulBasisService(
            _registrationRepository, _liaRepository, _registrationReadModels, _liaReadModels,
            null!, _timeProvider, _serviceLogger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("cache");
    }

    [Fact]
    public void DefaultLawfulBasisService_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLawfulBasisService(
            _registrationRepository, _liaRepository, _registrationReadModels, _liaReadModels,
            _cache, null!, _serviceLogger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void DefaultLawfulBasisService_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLawfulBasisService(
            _registrationRepository, _liaRepository, _registrationReadModels, _liaReadModels,
            _cache, _timeProvider, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region LawfulBasisHealthCheck Guards

    [Fact]
    public void LawfulBasisHealthCheck_NullServiceProvider_ThrowsArgumentNullException()
    {
        var logger = NullLogger<LawfulBasisHealthCheck>.Instance;

        var act = () => new LawfulBasisHealthCheck(null!, logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void LawfulBasisHealthCheck_NullLogger_ThrowsArgumentNullException()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();

        var act = () => new LawfulBasisHealthCheck(serviceProvider, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region LawfulBasisValidationPipelineBehavior Guards

    [Fact]
    public void LawfulBasisValidationPipelineBehavior_NullService_ThrowsArgumentNullException()
    {
        var extractor = Substitute.For<ILawfulBasisSubjectIdExtractor>();
        var options = Options.Create(new LawfulBasisOptions());
        var logger = NullLogger<LawfulBasisValidationPipelineBehavior<TestRequest, TestResponse>>.Instance;

        var act = () => new LawfulBasisValidationPipelineBehavior<TestRequest, TestResponse>(
            null!, extractor, options, logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("service");
    }

    [Fact]
    public void LawfulBasisValidationPipelineBehavior_NullSubjectIdExtractor_ThrowsArgumentNullException()
    {
        var service = Substitute.For<ILawfulBasisService>();
        var options = Options.Create(new LawfulBasisOptions());
        var logger = NullLogger<LawfulBasisValidationPipelineBehavior<TestRequest, TestResponse>>.Instance;

        var act = () => new LawfulBasisValidationPipelineBehavior<TestRequest, TestResponse>(
            service, null!, options, logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("subjectIdExtractor");
    }

    [Fact]
    public void LawfulBasisValidationPipelineBehavior_NullOptions_ThrowsArgumentNullException()
    {
        var service = Substitute.For<ILawfulBasisService>();
        var extractor = Substitute.For<ILawfulBasisSubjectIdExtractor>();
        var logger = NullLogger<LawfulBasisValidationPipelineBehavior<TestRequest, TestResponse>>.Instance;

        var act = () => new LawfulBasisValidationPipelineBehavior<TestRequest, TestResponse>(
            service, extractor, null!, logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void LawfulBasisValidationPipelineBehavior_NullLogger_ThrowsArgumentNullException()
    {
        var service = Substitute.For<ILawfulBasisService>();
        var extractor = Substitute.For<ILawfulBasisSubjectIdExtractor>();
        var options = Options.Create(new LawfulBasisOptions());

        var act = () => new LawfulBasisValidationPipelineBehavior<TestRequest, TestResponse>(
            service, extractor, options, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void LawfulBasisValidationPipelineBehavior_NullConsentProvider_DoesNotThrow()
    {
        var service = Substitute.For<ILawfulBasisService>();
        var extractor = Substitute.For<ILawfulBasisSubjectIdExtractor>();
        var options = Options.Create(new LawfulBasisOptions());
        var logger = NullLogger<LawfulBasisValidationPipelineBehavior<TestRequest, TestResponse>>.Instance;

        var act = () => new LawfulBasisValidationPipelineBehavior<TestRequest, TestResponse>(
            service, extractor, options, logger, consentProvider: null);

        Should.NotThrow(act);
    }

    #endregion

    #region DefaultLawfulBasisProvider Guards

    [Fact]
    public void DefaultLawfulBasisProvider_NullService_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLawfulBasisProvider(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("service");
    }

    #endregion

    #region LawfulBasisAutoRegistrationHostedService Guards

    [Fact]
    public void LawfulBasisAutoRegistrationHostedService_NullService_ThrowsArgumentNullException()
    {
        var descriptor = new LawfulBasisAutoRegistrationDescriptor([], new Dictionary<Type, GDPRLawfulBasis>());
        var logger = NullLogger<LawfulBasisAutoRegistrationHostedService>.Instance;

        var act = () => new LawfulBasisAutoRegistrationHostedService(null!, descriptor, logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("service");
    }

    [Fact]
    public void LawfulBasisAutoRegistrationHostedService_NullDescriptor_ThrowsArgumentNullException()
    {
        var service = Substitute.For<ILawfulBasisService>();
        var logger = NullLogger<LawfulBasisAutoRegistrationHostedService>.Instance;

        var act = () => new LawfulBasisAutoRegistrationHostedService(service, null!, logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("descriptor");
    }

    [Fact]
    public void LawfulBasisAutoRegistrationHostedService_NullLogger_ThrowsArgumentNullException()
    {
        var service = Substitute.For<ILawfulBasisService>();
        var descriptor = new LawfulBasisAutoRegistrationDescriptor([], new Dictionary<Type, GDPRLawfulBasis>());

        var act = () => new LawfulBasisAutoRegistrationHostedService(service, descriptor, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Minimal test request for pipeline behavior guard tests.
    /// </summary>
    private sealed record TestRequest : IRequest<TestResponse>;

    /// <summary>
    /// Minimal test response for pipeline behavior guard tests.
    /// </summary>
    private sealed record TestResponse;

    #endregion
}
