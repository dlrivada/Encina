```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 11.315 ns | 0.0535 ns | 0.0501 ns |  1.38 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.877 ns | 0.0648 ns | 0.0606 ns |  1.57 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.829 ns | 0.0352 ns | 0.0330 ns |  1.08 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  9.073 ns | 0.0258 ns | 0.0229 ns |  1.11 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  9.951 ns | 0.0267 ns | 0.0250 ns |  1.21 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 41.251 ns | 0.0775 ns | 0.0725 ns |  5.03 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 50.612 ns | 0.3368 ns | 0.3150 ns |  6.17 |    0.04 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.204 ns | 0.0311 ns | 0.0291 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 11.366 ns | 0.3885 ns | 0.0213 ns |  1.38 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.211 ns | 0.4064 ns | 0.0223 ns |  1.61 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.087 ns | 0.1625 ns | 0.0089 ns |  1.11 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  9.274 ns | 1.2650 ns | 0.0693 ns |  1.13 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 10.079 ns | 0.2700 ns | 0.0148 ns |  1.23 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 41.284 ns | 0.9351 ns | 0.0513 ns |  5.03 |    0.02 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 49.237 ns | 0.9652 ns | 0.0529 ns |  6.00 |    0.02 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.208 ns | 0.6587 ns | 0.0361 ns |  1.00 |    0.01 |         - |          NA |
