```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.85 ns | 0.021 ns | 0.019 ns |  1.42 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.34 ns | 0.030 ns | 0.025 ns |  1.64 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.17 ns | 0.005 ns | 0.004 ns |  1.09 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.73 ns | 0.009 ns | 0.007 ns |  1.23 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.98 ns | 0.009 ns | 0.008 ns |  1.34 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.62 ns | 0.039 ns | 0.035 ns |  5.53 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.66 ns | 0.057 ns | 0.048 ns |  6.34 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.15 ns | 0.006 ns | 0.005 ns |  1.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 15.84 ns | 0.016 ns | 0.023 ns |  1.38 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 18.32 ns | 0.010 ns | 0.014 ns |  1.60 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.41 ns | 0.005 ns | 0.006 ns |  1.08 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.42 ns | 0.010 ns | 0.014 ns |  1.17 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 14.67 ns | 0.007 ns | 0.009 ns |  1.28 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 60.38 ns | 0.021 ns | 0.029 ns |  5.27 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 71.02 ns | 0.093 ns | 0.124 ns |  6.20 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.46 ns | 0.006 ns | 0.008 ns |  1.00 |         - |          NA |
