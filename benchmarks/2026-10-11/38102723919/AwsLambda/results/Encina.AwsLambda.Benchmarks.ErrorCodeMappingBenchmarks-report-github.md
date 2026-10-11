```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.96 ns | 0.016 ns | 0.012 ns |  1.42 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.95 ns | 0.010 ns | 0.009 ns |  1.61 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.17 ns | 0.006 ns | 0.005 ns |  1.15 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.73 ns | 0.017 ns | 0.013 ns |  1.30 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.98 ns | 0.013 ns | 0.011 ns |  1.42 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.18 ns | 0.046 ns | 0.041 ns |  5.32 |    0.00 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.69 ns | 0.047 ns | 0.037 ns |  6.69 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.56 ns | 0.008 ns | 0.006 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 14.80 ns | 0.012 ns | 0.017 ns |  1.37 |    0.07 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 16.38 ns | 0.022 ns | 0.032 ns |  1.51 |    0.07 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.41 ns | 0.015 ns | 0.021 ns |  1.15 |    0.05 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.42 ns | 0.008 ns | 0.012 ns |  1.24 |    0.06 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 14.67 ns | 0.010 ns | 0.015 ns |  1.35 |    0.06 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 55.43 ns | 0.025 ns | 0.036 ns |  5.12 |    0.24 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 71.03 ns | 0.040 ns | 0.057 ns |  6.56 |    0.31 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 10.86 ns | 0.385 ns | 0.526 ns |  1.00 |    0.07 |         - |          NA |
