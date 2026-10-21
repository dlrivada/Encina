```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.855 ns | 0.1346 ns | 0.1124 ns |  1.53 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 11.510 ns | 0.2458 ns | 0.3019 ns |  1.79 |    0.07 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  7.312 ns | 0.1441 ns | 0.1348 ns |  1.14 |    0.04 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.903 ns | 0.1797 ns | 0.1923 ns |  1.23 |    0.05 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 37.834 ns | 0.7510 ns | 0.7025 ns |  5.89 |    0.20 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 43.102 ns | 0.7407 ns | 0.6928 ns |  6.70 |    0.22 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.434 ns | 0.1458 ns | 0.1946 ns |  1.00 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.557 ns | 0.0968 ns | 0.0809 ns |  1.02 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 10.085 ns | 3.1125 ns | 0.1706 ns |  1.61 |    0.07 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 11.337 ns | 5.3898 ns | 0.2954 ns |  1.81 |    0.08 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  7.835 ns | 6.4277 ns | 0.3523 ns |  1.25 |    0.07 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.497 ns | 4.8778 ns | 0.2674 ns |  1.36 |    0.07 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 34.868 ns | 5.1788 ns | 0.2839 ns |  5.57 |    0.23 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 39.826 ns | 3.6475 ns | 0.1999 ns |  6.36 |    0.26 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.269 ns | 5.5717 ns | 0.3054 ns |  1.00 |    0.06 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.597 ns | 0.6513 ns | 0.0357 ns |  1.05 |    0.04 |         - |          NA |
