```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  7.880 ns | 0.0557 ns | 0.0465 ns |  1.46 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  8.842 ns | 0.1165 ns | 0.0973 ns |  1.63 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  5.754 ns | 0.0114 ns | 0.0095 ns |  1.06 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.125 ns | 0.0261 ns | 0.0218 ns |  1.13 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.213 ns | 0.0579 ns | 0.0513 ns |  1.33 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 28.755 ns | 0.1731 ns | 0.1534 ns |  5.31 |    0.03 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 34.620 ns | 0.2053 ns | 0.1820 ns |  6.39 |    0.04 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.415 ns | 0.0216 ns | 0.0202 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  7.906 ns | 0.7166 ns | 0.0393 ns |  1.46 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  8.891 ns | 0.3842 ns | 0.0211 ns |  1.64 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.268 ns | 0.3265 ns | 0.0179 ns |  1.15 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.244 ns | 1.2288 ns | 0.0674 ns |  1.15 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.548 ns | 2.6011 ns | 0.1426 ns |  1.39 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 29.024 ns | 0.6110 ns | 0.0335 ns |  5.34 |    0.04 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 35.127 ns | 2.8790 ns | 0.1578 ns |  6.47 |    0.06 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.434 ns | 0.9569 ns | 0.0525 ns |  1.00 |    0.01 |         - |          NA |
