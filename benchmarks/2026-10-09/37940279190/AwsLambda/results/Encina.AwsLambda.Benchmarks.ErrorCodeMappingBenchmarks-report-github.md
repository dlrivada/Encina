```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.75GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.99 ns | 0.066 ns | 0.062 ns |  1.42 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.95 ns | 0.015 ns | 0.012 ns |  1.60 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.24 ns | 0.010 ns | 0.009 ns |  1.16 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.73 ns | 0.011 ns | 0.009 ns |  1.30 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.97 ns | 0.010 ns | 0.009 ns |  1.41 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.24 ns | 0.106 ns | 0.094 ns |  5.32 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.72 ns | 0.054 ns | 0.048 ns |  6.68 |    0.02 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.58 ns | 0.040 ns | 0.034 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.84 ns | 1.187 ns | 0.065 ns |  1.44 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.41 ns | 0.538 ns | 0.029 ns |  1.59 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.00 ns | 1.681 ns | 0.092 ns |  1.26 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.43 ns | 0.258 ns | 0.014 ns |  1.30 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.67 ns | 0.090 ns | 0.005 ns |  1.42 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 56.17 ns | 0.710 ns | 0.039 ns |  5.44 |    0.00 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 70.94 ns | 1.597 ns | 0.088 ns |  6.87 |    0.01 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.33 ns | 0.070 ns | 0.004 ns |  1.00 |    0.00 |         - |          NA |
