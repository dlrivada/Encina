# Encina.Dapr

**Dapr service mesh integration for Encina with Railway Oriented Programming.**

Integrate your Encina applications with Dapr's distributed application runtime building blocks: service invocation, pub/sub, state management, bindings, and secrets.

---

## Features

✅ **Service-to-Service Invocation** - Call microservices with automatic service discovery
✅ **Pub/Sub** - Cloud-agnostic event publishing (Redis, RabbitMQ, Azure Service Bus, Kafka)
✅ **State Management** - Key/value storage with strong/eventual consistency, transactions, TTL
✅ **Bindings** - 100+ input/output bindings for external systems (queues, databases, APIs)
✅ **Secrets** - Secure secret retrieval from Azure Key Vault, AWS Secrets Manager, HashiCorp Vault
✅ **Railway Oriented Programming** - All operations return `Either<MediatorError, T>`
✅ **Zero Configuration** - Works with default DaprClient or custom configuration
✅ **LoggerMessage** - High-performance logging with source generators

---

## Installation

```bash
dotnet add package Encina.Dapr
```

**Prerequisites**: Dapr CLI installed and initialized. See [Dapr Getting Started](https://docs.dapr.io/getting-started/).

---

## Quick Start

### 1. Register Encina with Dapr

```csharp
services.AddEncina(config => { });
services.AddEncinaDapr(); // Uses default DaprClient

// Or with custom DaprClient configuration
services.AddEncinaDapr(builder =>
{
    builder.UseHttpEndpoint("http://localhost:3500")
           .UseGrpcEndpoint("http://localhost:50001");
});
```

### 2. Service-to-Service Invocation

Call other microservices with automatic service discovery and load balancing:

```csharp
// Define the request
public record GetInventoryQuery(int ProductId)
    : IQuery<Stock>, IDaprServiceInvocationRequest<Stock>
{
    public string AppId => "inventory-service";
    public string MethodName => "inventory";
    public HttpMethod HttpMethod => HttpMethod.Get;

    public async Task<Stock> InvokeAsync(DaprClient daprClient, CancellationToken ct)
    {
        return await daprClient.InvokeMethodAsync<Stock>(
            AppId,
            MethodName,
            ct);
    }
}

// Use via Encina
var result = await mediator.Send(new GetInventoryQuery(productId), ct);
result.Match(
    Right: stock => Console.WriteLine($"Stock: {stock.Quantity}"),
    Left: error => Console.WriteLine($"Error: {error.Message}")
);
```

### 3. Pub/Sub Event Publishing

Publish events to any message broker (Redis, RabbitMQ, Kafka, Azure Service Bus):

```csharp
// Define the event
public record OrderPlacedEvent(string OrderId, decimal Total)
    : INotification, IDaprPubSubRequest
{
    public string PubSubName => "pubsub";
    public string TopicName => "orders";

    public async Task PublishAsync(DaprClient daprClient, CancellationToken ct)
    {
        await daprClient.PublishEventAsync(
            PubSubName,
            TopicName,
            new { OrderId, Total },
            ct);
    }
}

// Publish via Encina
var result = await mediator.Publish(new OrderPlacedEvent(orderId, total), ct);
result.Match(
    Right: _ => Console.WriteLine("Event published"),
    Left: error => Console.WriteLine($"Error: {error.Message}")
);
```

### 4. State Management

Store and retrieve state with any state store (Redis, Cosmos DB, SQL Server):

```csharp
// Save state
public record SaveUserPreferencesCommand(string UserId, UserPreferences Preferences)
    : ICommand<Unit>, IDaprStateRequest<Unit>
{
    public string StoreName => "statestore";
    public string StateKey => $"user-preferences-{UserId}";

    public async Task<Unit> ExecuteAsync(DaprClient daprClient, CancellationToken ct)
    {
        await daprClient.SaveStateAsync(StoreName, StateKey, Preferences, cancellationToken: ct);
        return Unit.Default;
    }
}

// Get state
public record GetUserPreferencesQuery(string UserId)
    : IQuery<UserPreferences>, IDaprStateRequest<UserPreferences>
{
    public string StoreName => "statestore";
    public string StateKey => $"user-preferences-{UserId}";

    public async Task<UserPreferences> ExecuteAsync(DaprClient daprClient, CancellationToken ct)
    {
        return await daprClient.GetStateAsync<UserPreferences>(
            StoreName,
            StateKey,
            cancellationToken: ct);
    }
}

// Use via Encina
var saveResult = await mediator.Send(new SaveUserPreferencesCommand(userId, prefs), ct);
var getResult = await mediator.Send(new GetUserPreferencesQuery(userId), ct);
```

### 5. Bindings (Input/Output)

Integrate with 100+ external systems (SendGrid, Twilio, AWS S3, Azure Blob Storage):

```csharp
// Send email via SendGrid binding
public record SendEmailCommand(string To, string Subject, string Body)
    : ICommand<Unit>, IDaprBindingRequest<Unit>
{
    public string BindingName => "sendgrid";
    public string Operation => "create";

    public async Task<Unit> ExecuteAsync(DaprClient daprClient, CancellationToken ct)
    {
        var metadata = new Dictionary<string, string>
        {
            ["emailTo"] = To,
            ["subject"] = Subject
        };

        await daprClient.InvokeBindingAsync(BindingName, Operation, Body, metadata, ct);
        return Unit.Default;
    }
}

// Upload to Azure Blob Storage
public record UploadFileCommand(string FileName, byte[] FileContent)
    : ICommand<Unit>, IDaprBindingRequest<Unit>
{
    public string BindingName => "azure-blob";
    public string Operation => "create";

    public async Task<Unit> ExecuteAsync(DaprClient daprClient, CancellationToken ct)
    {
        var metadata = new Dictionary<string, string> { ["blobName"] = FileName };
        await daprClient.InvokeBindingAsync(BindingName, Operation, FileContent, metadata, ct);
        return Unit.Default;
    }
}
```

### 6. Secrets Management

Retrieve secrets securely from Azure Key Vault, AWS Secrets Manager, HashiCorp Vault:

```csharp
// Retrieve database connection string
public record GetDatabaseConnectionStringQuery()
    : IQuery<string>, IDaprSecretRequest<string>
{
    public string SecretStoreName => "azurekeyvault";
    public string SecretName => "database-connection-string";
    public string? SecretKey => null;

    public async Task<string> ExecuteAsync(DaprClient daprClient, CancellationToken ct)
    {
        var secrets = await daprClient.GetSecretAsync(
            SecretStoreName,
            SecretName,
            cancellationToken: ct);

        return secrets.TryGetValue(SecretName, out var value)
            ? value
            : throw new KeyNotFoundException($"Secret '{SecretName}' not found");
    }
}

// Retrieve API credentials (multi-value secret)
public record GetApiCredentialsQuery()
    : IQuery<ApiCredentials>, IDaprSecretRequest<ApiCredentials>
{
    public string SecretStoreName => "hashicorp-vault";
    public string SecretName => "api-credentials";
    public string? SecretKey => null;

    public async Task<ApiCredentials> ExecuteAsync(DaprClient daprClient, CancellationToken ct)
    {
        var secrets = await daprClient.GetSecretAsync(SecretStoreName, SecretName, cancellationToken: ct);
        return new ApiCredentials
        {
            ApiKey = secrets["api-key"],
            ApiSecret = secrets["api-secret"],
            Endpoint = secrets["endpoint"]
        };
    }
}
```

---

## Dapr Components Configuration

### 1. Pub/Sub Component (Redis)

Create `components/pubsub.yaml`:

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: pubsub
spec:
  type: pubsub.redis
  version: v1
  metadata:
  - name: redisHost
    value: localhost:6379
  - name: redisPassword
    value: ""
```

### 2. State Store Component (Redis)

Create `components/statestore.yaml`:

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: statestore
spec:
  type: state.redis
  version: v1
  metadata:
  - name: redisHost
    value: localhost:6379
  - name: redisPassword
    value: ""
```

### 3. Secret Store Component (Azure Key Vault)

Create `components/azurekeyvault.yaml`:

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: azurekeyvault
spec:
  type: secretstores.azure.keyvault
  version: v1
  metadata:
  - name: vaultName
    value: "your-keyvault-name"
  - name: azureTenantId
    value: "your-tenant-id"
  - name: azureClientId
    value: "your-client-id"
  - name: azureClientSecret
    value: "your-client-secret"
```

### 4. Binding Component (SendGrid)

Create `components/sendgrid.yaml`:

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: sendgrid
spec:
  type: bindings.twilio.sendgrid
  version: v1
  metadata:
  - name: apiKey
    value: "your-sendgrid-api-key"
  - name: emailFrom
    value: "noreply@example.com"
  - name: emailFromName
    value: "My App"
```

---

## Local Development

Run your app with Dapr sidecar:

```bash
dapr run --app-id myapp --app-port 5000 --dapr-http-port 3500 --components-path ./components -- dotnet run
```

---

## Production Deployment (Kubernetes)

Deploy with Dapr Kubernetes operator:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp
spec:
  replicas: 3
  template:
    metadata:
      annotations:
        dapr.io/enabled: "true"
        dapr.io/app-id: "myapp"
        dapr.io/app-port: "80"
    spec:
      containers:
      - name: myapp
        image: myapp:latest
        ports:
        - containerPort: 80
```

---

## Benefits Over Direct DaprClient Usage

| Feature | Direct DaprClient | Encina.Dapr |
|---------|------------------|---------------------|
| **Railway Oriented Programming** | ❌ Throws exceptions | ✅ Returns `Either<MediatorError, T>` |
| **Centralized Error Handling** | ❌ Scattered try/catch | ✅ Pipeline behaviors |
| **Logging & Telemetry** | ❌ Manual | ✅ Automatic via behaviors |
| **Validation** | ❌ Manual | ✅ FluentValidation integration |
| **Caching** | ❌ Manual | ✅ Encina.Caching integration |
| **Testing** | ⚠️ Mock DaprClient | ✅ Mock IMediator |
| **Consistency** | ⚠️ Mixed patterns | ✅ Uniform request/response |

---

## Resilience & Retry

Dapr handles resilience at the infrastructure level via resiliency policies:

Create `resiliency.yaml`:

```yaml
apiVersion: dapr.io/v1alpha1
kind: Resiliency
metadata:
  name: myapp-resiliency
spec:
  policies:
    retries:
      DefaultRetry:
        policy: exponential
        maxRetries: 3
        backOff:
          initialInterval: 1s
          maxInterval: 30s
    circuitBreakers:
      DefaultCircuitBreaker:
        failureThreshold: 0.5
        timeout: 30s
  targets:
    apps:
      inventory-service:
        retry: DefaultRetry
        circuitBreaker: DefaultCircuitBreaker
    components:
      statestore:
        retry: DefaultRetry
      pubsub:
        retry: DefaultRetry
```

No code changes needed - Dapr sidecar handles all retry logic automatically!

---

## When to Use Dapr vs Polly?

| Scenario | Use | Reason |
|----------|-----|--------|
| **Monolith** | Encina.Polly | No sidecar overhead |
| **Microservices (Docker)** | Encina.Dapr | Consistent resilience across all services |
| **Microservices (Kubernetes)** | Encina.Dapr | Native K8s integration |
| **Edge/IoT** | Encina.Dapr | Works beyond cloud |
| **Simple APIs** | Encina.Polly | Less infrastructure |
| **Complex Distributed Systems** | Encina.Dapr | Pub/sub, state, bindings included |

---

## Error Handling

All Dapr operations convert `DaprException` to `MediatorError`:

```csharp
var result = await mediator.Send(new GetInventoryQuery(productId), ct);
result.Match(
    Right: stock => ProcessStock(stock),
    Left: error =>
    {
        // error.Message contains detailed Dapr error info
        // error.Exception is the original DaprException
        logger.LogError(error.Exception, "Dapr call failed: {Message}", error.Message);
    }
);
```

---

## Dependencies

- **Dapr.Client** 1.16.1+
- **Dapr.AspNetCore** 1.16.1+
- **Encina** (core package)
- **LanguageExt.Core** (for `Either<L, R>`)

---

## Learn More

- [Dapr Documentation](https://docs.dapr.io/)
- [Dapr Building Blocks](https://docs.dapr.io/developing-applications/building-blocks/)
- [Encina Documentation](https://github.com/your-repo/Encina)
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)

---

## License

MIT License - See LICENSE file for details.
