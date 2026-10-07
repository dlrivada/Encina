```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.01GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.99 ns | 0.020 ns | 0.018 ns |  1.42 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.92 ns | 0.012 ns | 0.010 ns |  1.60 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.17 ns | 0.006 ns | 0.005 ns |  1.15 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.73 ns | 0.016 ns | 0.015 ns |  1.30 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.98 ns | 0.034 ns | 0.028 ns |  1.42 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.14 ns | 0.059 ns | 0.050 ns |  5.31 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.66 ns | 0.033 ns | 0.026 ns |  6.69 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.56 ns | 0.016 ns | 0.013 ns |  1.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.02 ns | 0.398 ns | 0.022 ns |  1.45 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.39 ns | 0.353 ns | 0.019 ns |  1.58 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.40 ns | 0.170 ns | 0.009 ns |  1.20 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.42 ns | 0.037 ns | 0.002 ns |  1.29 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.66 ns | 0.175 ns | 0.010 ns |  1.41 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.36 ns | 0.114 ns | 0.006 ns |  5.33 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 73.02 ns | 0.346 ns | 0.019 ns |  7.04 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.38 ns | 0.128 ns | 0.007 ns |  1.00 |         - |          NA |
