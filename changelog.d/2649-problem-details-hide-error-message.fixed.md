- **Problem Details no longer expose `EncinaError.Message` to HTTP clients** (#2649). `ProblemDetailsExtensions.ToProblemDetails` and `ToActionResult` (Encina.AspNetCore) and the `api` template of `encina new` return the status, a generic title, `errorCode` and `traceId`/`correlationId`; `detail` is omitted, so 400 validation responses no longer list field messages.

  The new `EncinaAspNetCoreOptions.IncludeErrorMessageInProblemDetails` (default `false`) puts the message in `detail` and the exception type, message and stack trace in the `exception` extension only when the host environment is Development. Elsewhere it is ignored and a startup warning is logged (EventIds 205-206). The `api` template now maps errors with `ToProblemDetails(httpContext)`.

- **Removed `EncinaAspNetCoreOptions.IncludeExceptionDetails`** and the environment-name check: the `exception` extension is governed by `IncludeErrorMessageInProblemDetails` and the same Development-only guard (#2649).
