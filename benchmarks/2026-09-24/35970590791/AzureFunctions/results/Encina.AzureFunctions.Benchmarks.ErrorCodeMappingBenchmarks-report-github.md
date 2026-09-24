```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.57GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.511 ns |  0.1102 ns | 0.0920 ns |  1.49 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.276 ns |  0.0657 ns | 0.0549 ns |  1.46 |    0.04 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.485 ns |  0.0783 ns | 0.0654 ns |  1.25 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.027 ns |  0.1520 ns | 0.1422 ns |  1.31 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.214 ns |  0.0695 ns | 0.0543 ns |  5.50 |    0.15 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.679 ns |  0.6702 ns | 0.6269 ns |  6.74 |    0.19 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.409 ns |  0.2004 ns | 0.2308 ns |  1.00 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.512 ns |  0.1551 ns | 0.1451 ns |  1.13 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.704 ns |  6.1379 ns | 0.3364 ns |  1.50 |    0.07 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.099 ns |  1.2270 ns | 0.0673 ns |  1.55 |    0.07 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 11.506 ns |  1.3337 ns | 0.0731 ns |  1.36 |    0.06 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.388 ns |  3.4626 ns | 0.1898 ns |  1.35 |    0.06 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 44.829 ns |  2.4558 ns | 0.1346 ns |  5.30 |    0.23 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 56.196 ns | 12.5302 ns | 0.6868 ns |  6.64 |    0.30 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.480 ns |  7.9722 ns | 0.4370 ns |  1.00 |    0.06 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.668 ns |  5.3850 ns | 0.2952 ns |  1.14 |    0.06 |         - |          NA |
