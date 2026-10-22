```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.276 ns |  0.1768 ns | 0.1477 ns |  1.49 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.353 ns |  0.1889 ns | 0.1674 ns |  1.67 |    0.04 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.932 ns |  0.1388 ns | 0.1652 ns |  1.12 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.794 ns |  0.1683 ns | 0.1729 ns |  1.25 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 35.339 ns |  0.4994 ns | 0.4427 ns |  5.69 |    0.12 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 41.532 ns |  0.4098 ns | 0.3633 ns |  6.68 |    0.12 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.216 ns |  0.1151 ns | 0.1077 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.643 ns |  0.1561 ns | 0.1671 ns |  1.07 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.778 ns |  4.2327 ns | 0.2320 ns |  1.57 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.946 ns |  1.1284 ns | 0.0619 ns |  1.76 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.901 ns |  1.7175 ns | 0.0941 ns |  1.11 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.177 ns |  3.8141 ns | 0.2091 ns |  1.32 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 36.724 ns | 22.1352 ns | 1.2133 ns |  5.91 |    0.19 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 40.839 ns |  7.6253 ns | 0.4180 ns |  6.57 |    0.10 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.218 ns |  1.6783 ns | 0.0920 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.697 ns |  2.0775 ns | 0.1139 ns |  1.08 |    0.02 |         - |          NA |
