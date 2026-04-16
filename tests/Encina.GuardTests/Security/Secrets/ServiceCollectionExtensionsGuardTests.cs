using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Configuration;
using Shouldly;
using Microsoft.Extensions.Configuration;

namespace Encina.GuardTests.Security.Secrets;

/// <summary>
/// Guard clause tests for <see cref="ServiceCollectionExtensions"/>.
/// Verifies that null arguments are properly rejected in all public extension methods.
/// </summary>
public sealed class ServiceCollectionExtensionsGuardTests
{
    #region AddEncinaSecrets(IServiceCollection, Action?) Guards

    [Fact]
    public void AddEncinaSecrets_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaSecrets();

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaSecrets_WithConfigure_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaSecrets(opts => opts.EnableCaching = true);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    #endregion

    #region AddEncinaSecrets<TReader>(IServiceCollection, Action?) Guards

    [Fact]
    public void AddEncinaSecretsGeneric_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaSecrets<FakeSecretReader>();

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaSecretsGeneric_WithConfigure_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaSecrets<FakeSecretReader>(
            opts => opts.EnableCaching = false);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    #endregion

    #region AddSecretRotationHandler<THandler> Guards

    [Fact]
    public void AddSecretRotationHandler_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddSecretRotationHandler<FakeRotationHandler>();

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    #endregion

    #region AddEncinaSecrets(IConfigurationBuilder, IServiceProvider, Action?) Guards

    [Fact]
    public void AddEncinaSecretsConfig_NullBuilder_ThrowsArgumentNullException()
    {
        IConfigurationBuilder builder = null!;
        var sp = Substitute.For<IServiceProvider>();

        var act = () => builder.AddEncinaSecrets(sp);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("builder");
    }

    [Fact]
    public void AddEncinaSecretsConfig_NullServiceProvider_ThrowsArgumentNullException()
    {
        var builder = new ConfigurationBuilder();

        var act = () => builder.AddEncinaSecrets(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("serviceProvider");
    }

    #endregion

    #region Fakes

    private sealed class FakeSecretReader : ISecretReader
    {
        public ValueTask<Either<EncinaError, string>> GetSecretAsync(
            string secretName, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<Either<EncinaError, string>>("fake");

        public ValueTask<Either<EncinaError, T>> GetSecretAsync<T>(
            string secretName, CancellationToken cancellationToken = default) where T : class
            => throw new NotImplementedException();
    }

    private sealed class FakeRotationHandler : ISecretRotationHandler
    {
        public ValueTask<Either<EncinaError, string>> GenerateNewSecretAsync(
            string secretName, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<Either<EncinaError, string>>("new-secret");

        public ValueTask<Either<EncinaError, LanguageExt.Unit>> OnRotationAsync(
            string secretName, string oldValue, string newValue, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<Either<EncinaError, LanguageExt.Unit>>(LanguageExt.Unit.Default);
    }

    #endregion
}
