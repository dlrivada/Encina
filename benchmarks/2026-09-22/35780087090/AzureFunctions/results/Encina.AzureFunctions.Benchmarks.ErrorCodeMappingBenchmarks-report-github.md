```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.01 ns | 0.060 ns | 0.056 ns |  1.42 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.96 ns | 0.018 ns | 0.014 ns |  1.61 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.73 ns | 0.011 ns | 0.009 ns |  1.30 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.97 ns | 0.008 ns | 0.007 ns |  1.42 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.12 ns | 0.033 ns | 0.028 ns |  5.32 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.73 ns | 0.096 ns | 0.080 ns |  6.70 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.56 ns | 0.007 ns | 0.006 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.16 ns | 0.008 ns | 0.007 ns |  1.15 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.89 ns | 0.879 ns | 0.048 ns |  1.41 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.37 ns | 0.637 ns | 0.035 ns |  1.55 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.41 ns | 0.066 ns | 0.004 ns |  1.27 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.67 ns | 0.274 ns | 0.015 ns |  1.39 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.40 ns | 0.682 ns | 0.037 ns |  5.26 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 70.90 ns | 0.152 ns | 0.008 ns |  6.73 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.54 ns | 0.130 ns | 0.007 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.42 ns | 0.466 ns | 0.026 ns |  1.18 |         - |          NA |
