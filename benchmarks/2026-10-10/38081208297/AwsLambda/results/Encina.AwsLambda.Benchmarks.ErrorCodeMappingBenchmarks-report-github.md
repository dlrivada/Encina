```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.691 ns |  0.1287 ns | 0.1075 ns |  1.55 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  9.703 ns |  0.1580 ns | 0.1400 ns |  1.74 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.164 ns |  0.1439 ns | 0.1657 ns |  1.10 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.595 ns |  0.0744 ns | 0.0622 ns |  1.18 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.681 ns |  0.0482 ns | 0.0403 ns |  1.37 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 32.484 ns |  0.2135 ns | 0.1782 ns |  5.81 |    0.10 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 38.714 ns |  0.1271 ns | 0.1189 ns |  6.92 |    0.12 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.592 ns |  0.1086 ns | 0.0963 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.406 ns |  1.5109 ns | 0.0828 ns |  1.38 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  9.432 ns |  5.9364 ns | 0.3254 ns |  1.55 |    0.05 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.128 ns |  0.9979 ns | 0.0547 ns |  1.00 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.834 ns |  1.3896 ns | 0.0762 ns |  1.12 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.854 ns |  0.8741 ns | 0.0479 ns |  1.29 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 30.948 ns | 25.1370 ns | 1.3778 ns |  5.07 |    0.20 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 37.455 ns |  9.4848 ns | 0.5199 ns |  6.14 |    0.08 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.101 ns |  0.7079 ns | 0.0388 ns |  1.00 |    0.01 |         - |          NA |
