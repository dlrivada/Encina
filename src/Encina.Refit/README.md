# Encina.Refit

HTTP client integration for Encina using [Refit](https://github.com/reactiveui/refit). Routes Encina requests to REST APIs via strongly-typed interfaces.

## Usage

A request implements `IRestApiRequest<TApiClient, TResponse>`; `RestApiRequestHandler` resolves the Refit client and calls `ExecuteAsync`. Register the client with `AddEncinaRefitClient`, which also adds the message handler that reports the HTTP method of each call to the handler.

```csharp
public interface IGitHubApi
{
    [Get("/users/{username}")]
    Task<GitHubUser> GetUserAsync(string username);
}

public record GetGitHubUserRequest(string Username)
    : IRestApiRequest<IGitHubApi, GitHubUser>
{
    public Task<GitHubUser> ExecuteAsync(IGitHubApi api, CancellationToken cancellationToken)
        => api.GetUserAsync(Username);
}

services.AddEncinaRefitClient<IGitHubApi>(client =>
    client.BaseAddress = new Uri("https://api.github.com"));
```

## Outbound errors

Every failure is a `Left` with an `EncinaError` whose code is one of `Encina.Recoverability.OutboundErrorCodes` and whose metadata uses `OutboundErrorMetadataKeys`, so the retry layers classify it without parsing text.

| Code | When | Transient |
|---|---|---|
| `encina.outbound.client_error` | other 4xx (400, 401, 403, 404, ...), 1xx, 3xx | no |
| `encina.outbound.server_error` | 5xx | yes |
| `encina.outbound.rate_limited` | 429 | yes |
| `encina.outbound.timeout` | the call timed out and the caller did not cancel; 408 | yes |
| `encina.outbound.network_error` | an `HttpRequestException` without a status (DNS, connection, TLS, proxy tunnel, broken connection) | yes |
| `encina.outbound.unexpected_response` | a 2xx response that could not be deserialized | no |
| `encina.outbound.unexpected` | any other exception | only `TimeoutException` and `IOException` |
| `encina.request.cancelled` | the caller cancelled (shared with the core code) | no |

| Metadata key | Type | Present |
|---|---|---|
| `http.status` | `int` | only when a response arrived |
| `http.retry_after` | `TimeSpan` | only when a valid `Retry-After` arrived (delay-seconds or HTTP-date; a date in the past is zero) |
| `transient` | `bool` | always |
| `outbound.ambiguous` | `bool` | only when true |

An outcome is ambiguous when the server may already have processed the request: on `POST` or `PATCH` (or when the method is unknown), a 500, 502 or 504, a timeout, or a network failure other than a DNS, connection, TLS or proxy-tunnel failure. It is still transient; the retry layers do not retry it automatically unless the request opts in (see the [Encina.Polly README](../Encina.Polly/README.md)).

The error never carries the response body: its message is fixed text with the status and reason phrase, the `ApiException` is not attached and exception text is not copied.

## Observability

The handler logs the correlation id of `IRequestContextAccessor.RequestContext`, emits the activity `Encina.Outbound.Send` and the counter `encina.outbound.requests_total` (tags: request name, error code, transient, HTTP status, `encina.tenant_id`). It never records a body, a message, a URL or a data-subject identifier. `IRequestContextAccessor` and `TimeProvider` are optional constructor parameters of the handler. Health checks for outbound endpoints are deferred to [#1233](https://github.com/dlrivada/Encina/issues/1233).

## Performance

> **Note**: Refit benchmarks are marked as **Unstable** due to inherent HTTP I/O variance. Values are indicative, not suitable for regression detection.

<!-- docref-table: bench:refit/* -->
<!-- /docref-table -->

*Auto-generated from benchmark data. See [methodology](../../docs/testing/performance-measurement-methodology.md).*
