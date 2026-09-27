using Encina.Compliance.LawfulBasis.Abstractions;
using Encina.Compliance.LawfulBasis.Health;
using Encina.Compliance.LawfulBasis.ReadModels;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // NSubstitute Returns with ValueTask

namespace Encina.UnitTests.Compliance.LawfulBasisModule;

/// <summary>
/// A raw <see cref="EncinaError.Message"/> or exception message must never reach
/// <see cref="LawfulBasisHealthCheck"/>'s results, per AGENTS.md's rule that
/// <c>EncinaError.Message</c> never reaches logs, activity tags, health-check results or
/// plaintext storage (#1461, sibling of #1435's pipeline-behavior fix).
/// </summary>
public sealed class LawfulBasisHealthCheckLeakTests
{
    private const string SecretRegistrationsErrorMessage = "SECRET-REGISTRATIONS-ERROR-MESSAGE-DO-NOT-LEAK";
    private const string SecretLiaErrorMessage = "SECRET-LIA-ERROR-MESSAGE-DO-NOT-LEAK";
    private const string SecretExceptionMessage = "SECRET-EXCEPTION-MESSAGE-DO-NOT-LEAK";

    [Fact]
    public async Task CheckHealthAsync_RegistrationsQueryFails_NeverCarriesRawErrorMessage()
    {
        var service = Substitute.For<ILawfulBasisService>();
        var error = EncinaErrors.Create("lawfulbasis.registrations_error", SecretRegistrationsErrorMessage);
        service.GetAllRegistrationsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IReadOnlyList<LawfulBasisReadModel>>>(
                Left<EncinaError, IReadOnlyList<LawfulBasisReadModel>>(error)));
        service.GetPendingLIAReviewsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IReadOnlyList<LIAReadModel>>>(
                Right<EncinaError, IReadOnlyList<LIAReadModel>>([])));

        var healthCheck = CreateHealthCheck(service);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        AssertNoSecret(result, SecretRegistrationsErrorMessage);
        result.Description!.ShouldContain("lawfulbasis.registrations_error");
    }

    [Fact]
    public async Task CheckHealthAsync_PendingLIAQueryFails_NeverCarriesRawErrorMessage()
    {
        var service = Substitute.For<ILawfulBasisService>();
        service.GetAllRegistrationsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IReadOnlyList<LawfulBasisReadModel>>>(
                Right<EncinaError, IReadOnlyList<LawfulBasisReadModel>>([])));
        var error = EncinaErrors.Create("lawfulbasis.lia_error", SecretLiaErrorMessage);
        service.GetPendingLIAReviewsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IReadOnlyList<LIAReadModel>>>(
                Left<EncinaError, IReadOnlyList<LIAReadModel>>(error)));

        var healthCheck = CreateHealthCheck(service);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        AssertNoSecret(result, SecretLiaErrorMessage);
        result.Description!.ShouldContain("lawfulbasis.lia_error");
    }

    [Fact]
    public async Task CheckHealthAsync_RegistrationsQueryThrows_NeverCarriesExceptionMessage()
    {
        var service = Substitute.For<ILawfulBasisService>();
        service.GetAllRegistrationsAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<LawfulBasisReadModel>>>>(
                _ => throw new InvalidOperationException(SecretExceptionMessage));

        var healthCheck = CreateHealthCheck(service);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        AssertNoSecret(result, SecretExceptionMessage);
        result.Data.ShouldContainKey("exception_type");
        result.Data["exception_type"].ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task CheckHealthAsync_PendingLIAQueryThrows_NeverCarriesExceptionMessage()
    {
        var service = Substitute.For<ILawfulBasisService>();
        service.GetAllRegistrationsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IReadOnlyList<LawfulBasisReadModel>>>(
                Right<EncinaError, IReadOnlyList<LawfulBasisReadModel>>([])));
        service.GetPendingLIAReviewsAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<LIAReadModel>>>>(
                _ => throw new InvalidOperationException(SecretExceptionMessage));

        var healthCheck = CreateHealthCheck(service);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        AssertNoSecret(result, SecretExceptionMessage);
        result.Description!.ShouldContain(nameof(InvalidOperationException));
    }

    private static LawfulBasisHealthCheck CreateHealthCheck(ILawfulBasisService service)
    {
        var services = new ServiceCollection();
        services.AddSingleton(service);
        return new LawfulBasisHealthCheck(
            services.BuildServiceProvider(),
            NullLogger<LawfulBasisHealthCheck>.Instance);
    }

    private static void AssertNoSecret(HealthCheckResult result, string secret)
    {
        (result.Description ?? string.Empty).ShouldNotContain(secret);
        result.Exception?.Message.ShouldNotContain(secret);

        foreach (var value in result.Data.Values)
        {
            switch (value)
            {
                case string text:
                    text.ShouldNotContain(secret);
                    break;
                case IEnumerable<string> texts:
                    texts.ShouldAllBe(t => !t.Contains(secret, StringComparison.Ordinal));
                    break;
            }
        }
    }
}
#pragma warning restore CA2012
