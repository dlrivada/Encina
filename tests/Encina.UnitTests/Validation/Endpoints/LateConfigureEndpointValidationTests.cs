using Encina.AmazonSQS;
using Encina.Cdc.Debezium;
using Encina.Cdc.MySql;
using Encina.Messaging.Encryption.AzureKeyVault;
using Encina.NATS;
using Encina.Security.Secrets.AzureKeyVault;
using Encina.Security.Secrets.HashiCorpVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using VaultSharp.V1.AuthMethods.Token;
using MessagingKeyVaultOptions = Encina.Messaging.Encryption.AzureKeyVault.AzureKeyVaultOptions;
using SecretsKeyVaultOptions = Encina.Security.Secrets.AzureKeyVault.AzureKeyVaultOptions;

namespace Encina.UnitTests.Validation.Endpoints;

/// <summary>
/// An endpoint changed after <c>Add*</c> (a later <c>services.Configure&lt;T&gt;</c>) escapes the eager
/// registration check, so the registered <see cref="IValidateOptions{TOptions}"/> must still reject it
/// at startup (<c>ValidateOnStart</c>) and on <see cref="IOptions{TOptions}.Value"/> (#852).
/// </summary>
public sealed class LateConfigureEndpointValidationTests
{
    public static TheoryData<string> Packages => new()
    {
        "HashiCorpVault",
        "SecretsAzureKeyVault",
        "MessagingAzureKeyVault",
        "AmazonSQS",
        "NATS",
        "MySqlCdc",
        "Debezium",
    };

    [Theory]
    [MemberData(nameof(Packages))]
    public async Task LateForbiddenEndpoint_FailsStartupValidation(string package)
    {
        var (services, _) = Register(package);
        await using var provider = services.BuildServiceProvider();

        var ex = Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IStartupValidator>().Validate());

        ex.Message.ShouldNotContain("s3cr3t");
    }

    [Theory]
    [MemberData(nameof(Packages))]
    public async Task LateForbiddenEndpoint_FailsOnOptionsValue(string package)
    {
        var (services, resolveOptions) = Register(package);
        await using var provider = services.BuildServiceProvider();

        Should.Throw<OptionsValidationException>(() => resolveOptions(provider));
    }

    // Registers the package with a valid endpoint, then overrides it with a forbidden one.
    private static (ServiceCollection Services, Action<IServiceProvider> ResolveOptions) Register(string package)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        Action<IServiceProvider> resolve = package switch
        {
            "HashiCorpVault" => RegisterHashiCorp(services),
            "SecretsAzureKeyVault" => RegisterSecretsKeyVault(services),
            "MessagingAzureKeyVault" => RegisterMessagingKeyVault(services),
            "AmazonSQS" => RegisterAmazonSqs(services),
            "NATS" => RegisterNats(services),
            "MySqlCdc" => RegisterMySqlCdc(services),
            "Debezium" => RegisterDebezium(services),
            _ => throw new ArgumentOutOfRangeException(nameof(package), package, null),
        };

        return (services, resolve);
    }

    private static Action<IServiceProvider> RegisterHashiCorp(ServiceCollection services)
    {
        services.AddHashiCorpVaultSecrets(v =>
        {
            v.VaultAddress = "https://vault.example.com:8200";
            v.AuthMethod = new TokenAuthMethodInfo("hvs.test");
        });
        services.Configure<HashiCorpVaultOptions>(v => v.VaultAddress = "https://user:s3cr3t@169.254.169.254/");
        return sp => _ = sp.GetRequiredService<IOptions<HashiCorpVaultOptions>>().Value;
    }

    private static Action<IServiceProvider> RegisterSecretsKeyVault(ServiceCollection services)
    {
        services.AddAzureKeyVaultSecrets(new Uri("https://my-vault.vault.azure.net/"));
        services.Configure<SecretsKeyVaultOptions>(o => o.VaultUri = new Uri("https://127.0.0.1/"));
        return sp => _ = sp.GetRequiredService<IOptions<SecretsKeyVaultOptions>>().Value;
    }

    private static Action<IServiceProvider> RegisterMessagingKeyVault(ServiceCollection services)
    {
        services.AddEncinaMessageEncryptionAzureKeyVault(o =>
        {
            o.VaultUri = new Uri("https://my-vault.vault.azure.net/");
            o.KeyName = "k";
        });
        services.Configure<MessagingKeyVaultOptions>(o => o.VaultUri = new Uri("http://my-vault.vault.azure.net/"));
        return sp => _ = sp.GetRequiredService<IOptions<MessagingKeyVaultOptions>>().Value;
    }

    private static Action<IServiceProvider> RegisterAmazonSqs(ServiceCollection services)
    {
        services.AddEncinaAmazonSQS(o => o.DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/orders");
        services.Configure<EncinaAmazonSQSOptions>(o => o.DefaultQueueUrl = "https://[::ffff:169.254.169.254]/latest");
        return sp => _ = sp.GetRequiredService<IOptions<EncinaAmazonSQSOptions>>().Value;
    }

    private static Action<IServiceProvider> RegisterNats(ServiceCollection services)
    {
        services.AddEncinaNATS(o => o.Url = "nats://nats.example.com:4222");
        services.Configure<EncinaNATSOptions>(o => o.Url = "nats://nats.example.com:4222,nats://localhost:4222");
        return sp => _ = sp.GetRequiredService<IOptions<EncinaNATSOptions>>().Value;
    }

    private static Action<IServiceProvider> RegisterMySqlCdc(ServiceCollection services)
    {
        services.AddEncinaCdcMySql(o =>
        {
            o.Hostname = "mysql.example.com";
            o.ConnectionString = "Server=mysql.example.com;Database=app";
        });
        services.Configure<MySqlCdcOptions>(o => o.ConnectionString = "Server=127.0.0.1;Password=s3cr3t");
        return sp => _ = sp.GetRequiredService<MySqlCdcOptions>();
    }

    private static Action<IServiceProvider> RegisterDebezium(ServiceCollection services)
    {
        services.AddEncinaCdcDebezium(_ => { });
        services.Configure<DebeziumCdcOptions>(o => o.ChannelCapacity = 0);
        return sp => _ = sp.GetRequiredService<DebeziumCdcOptions>();
    }
}
