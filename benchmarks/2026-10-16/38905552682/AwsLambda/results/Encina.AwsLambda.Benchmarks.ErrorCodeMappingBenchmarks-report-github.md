```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.051 ns |  0.1790 ns | 0.1989 ns |  1.43 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.575 ns |  0.1871 ns | 0.1750 ns |  1.67 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.650 ns |  0.1493 ns | 0.1834 ns |  1.05 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  7.106 ns |  0.1471 ns | 0.1376 ns |  1.12 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.692 ns |  0.1674 ns | 0.1484 ns |  1.21 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 35.346 ns |  0.6392 ns | 0.5667 ns |  5.57 |    0.12 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 40.390 ns |  0.3062 ns | 0.2557 ns |  6.37 |    0.11 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.344 ns |  0.1162 ns | 0.1030 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.488 ns |  5.5897 ns | 0.3064 ns |  1.44 |    0.06 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.645 ns |  2.2203 ns | 0.1217 ns |  1.62 |    0.05 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.743 ns |  1.9369 ns | 0.1062 ns |  1.03 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  7.001 ns |  1.2249 ns | 0.0671 ns |  1.07 |    0.03 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.106 ns |  0.6667 ns | 0.0365 ns |  1.23 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 35.398 ns | 14.7366 ns | 0.8078 ns |  5.39 |    0.20 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 41.004 ns | 16.5175 ns | 0.9054 ns |  6.24 |    0.23 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.578 ns |  4.1754 ns | 0.2289 ns |  1.00 |    0.04 |         - |          NA |
