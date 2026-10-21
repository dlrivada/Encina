```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.478 ns | 0.2053 ns | 0.2282 ns |  1.49 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.797 ns | 0.1149 ns | 0.0959 ns |  1.70 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.732 ns | 0.1221 ns | 0.1142 ns |  1.06 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.924 ns | 0.0800 ns | 0.0625 ns |  1.09 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.690 ns | 0.0868 ns | 0.0725 ns |  1.21 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 35.397 ns | 0.6071 ns | 0.5382 ns |  5.56 |    0.12 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 40.492 ns | 0.3478 ns | 0.3084 ns |  6.36 |    0.11 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.367 ns | 0.1229 ns | 0.1089 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.610 ns | 2.5332 ns | 0.1389 ns |  1.53 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.900 ns | 6.3009 ns | 0.3454 ns |  1.74 |    0.05 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.751 ns | 0.9851 ns | 0.0540 ns |  1.07 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  7.136 ns | 2.3246 ns | 0.1274 ns |  1.14 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.377 ns | 4.0356 ns | 0.2212 ns |  1.33 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 36.255 ns | 5.5442 ns | 0.3039 ns |  5.77 |    0.05 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 41.804 ns | 4.7611 ns | 0.2610 ns |  6.66 |    0.05 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.280 ns | 0.7129 ns | 0.0391 ns |  1.00 |    0.01 |         - |          NA |
