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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.98 ns | 0.059 ns | 0.049 ns |  1.40 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.95 ns | 0.029 ns | 0.026 ns |  1.59 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.18 ns | 0.011 ns | 0.010 ns |  1.14 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.75 ns | 0.049 ns | 0.043 ns |  1.29 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.97 ns | 0.007 ns | 0.006 ns |  1.40 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.20 ns | 0.062 ns | 0.055 ns |  5.26 |    0.07 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.70 ns | 0.049 ns | 0.038 ns |  6.61 |    0.09 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.69 ns | 0.156 ns | 0.146 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.80 ns | 0.339 ns | 0.019 ns |  1.32 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.38 ns | 0.327 ns | 0.018 ns |  1.47 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.67 ns | 8.339 ns | 0.457 ns |  1.13 |    0.04 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.43 ns | 0.465 ns | 0.025 ns |  1.20 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.67 ns | 0.085 ns | 0.005 ns |  1.31 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.42 ns | 1.081 ns | 0.059 ns |  4.96 |    0.04 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 71.18 ns | 8.187 ns | 0.449 ns |  6.37 |    0.07 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.18 ns | 2.108 ns | 0.116 ns |  1.00 |    0.01 |         - |          NA |
