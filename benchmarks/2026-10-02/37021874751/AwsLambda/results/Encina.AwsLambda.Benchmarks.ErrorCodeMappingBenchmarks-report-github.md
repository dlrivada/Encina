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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.94 ns | 0.016 ns | 0.014 ns |  1.40 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.95 ns | 0.013 ns | 0.011 ns |  1.59 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.17 ns | 0.010 ns | 0.009 ns |  1.14 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.73 ns | 0.009 ns | 0.008 ns |  1.29 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.97 ns | 0.007 ns | 0.006 ns |  1.40 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.18 ns | 0.062 ns | 0.049 ns |  5.27 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.64 ns | 0.034 ns | 0.030 ns |  6.63 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.66 ns | 0.021 ns | 0.018 ns |  1.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.82 ns | 1.520 ns | 0.083 ns |  1.43 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.37 ns | 0.600 ns | 0.033 ns |  1.58 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.45 ns | 0.992 ns | 0.054 ns |  1.20 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.48 ns | 1.662 ns | 0.091 ns |  1.30 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.67 ns | 0.148 ns | 0.008 ns |  1.42 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.45 ns | 0.682 ns | 0.037 ns |  5.36 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 70.92 ns | 0.829 ns | 0.045 ns |  6.86 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.34 ns | 0.279 ns | 0.015 ns |  1.00 |         - |          NA |
