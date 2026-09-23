```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.95 ns | 0.021 ns | 0.018 ns |  1.39 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.95 ns | 0.011 ns | 0.009 ns |  1.58 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.18 ns | 0.033 ns | 0.029 ns |  1.14 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.73 ns | 0.015 ns | 0.012 ns |  1.28 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.99 ns | 0.018 ns | 0.015 ns |  1.40 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.90 ns | 0.127 ns | 0.119 ns |  5.30 |    0.08 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.73 ns | 0.133 ns | 0.118 ns |  6.59 |    0.10 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.74 ns | 0.178 ns | 0.167 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.80 ns | 0.327 ns | 0.018 ns |  1.43 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.36 ns | 0.391 ns | 0.021 ns |  1.58 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.41 ns | 0.436 ns | 0.024 ns |  1.20 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.42 ns | 0.082 ns | 0.004 ns |  1.30 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.69 ns | 1.322 ns | 0.072 ns |  1.42 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.44 ns | 0.868 ns | 0.048 ns |  5.36 |    0.01 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 70.95 ns | 0.995 ns | 0.055 ns |  6.85 |    0.01 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.35 ns | 0.431 ns | 0.024 ns |  1.00 |    0.00 |         - |          NA |
