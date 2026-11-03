```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.974 ns |  0.1942 ns | 0.2159 ns |  1.49 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.388 ns |  0.1985 ns | 0.1760 ns |  1.73 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.319 ns |  0.0947 ns | 0.0791 ns |  1.05 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.631 ns |  0.1492 ns | 0.1659 ns |  1.10 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.524 ns |  0.1728 ns | 0.1990 ns |  1.25 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 33.951 ns |  0.3641 ns | 0.3040 ns |  5.65 |    0.09 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 38.798 ns |  0.4673 ns | 0.3902 ns |  6.46 |    0.11 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.009 ns |  0.0948 ns | 0.0841 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.392 ns |  2.1511 ns | 0.1179 ns |  1.53 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.572 ns |  5.1319 ns | 0.2813 ns |  1.72 |    0.05 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.731 ns |  3.5916 ns | 0.1969 ns |  1.10 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.941 ns |  6.1602 ns | 0.3377 ns |  1.13 |    0.05 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.622 ns |  2.0649 ns | 0.1132 ns |  1.24 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 33.176 ns |  3.3433 ns | 0.1833 ns |  5.41 |    0.09 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 40.046 ns | 60.8089 ns | 3.3331 ns |  6.53 |    0.48 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.136 ns |  2.1262 ns | 0.1165 ns |  1.00 |    0.02 |         - |          NA |
