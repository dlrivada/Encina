```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.92 ns | 0.177 ns | 0.148 ns |  1.50 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.31 ns | 0.065 ns | 0.058 ns |  1.73 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.56 ns | 0.249 ns | 0.233 ns |  1.28 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.92 ns | 0.055 ns | 0.052 ns |  1.41 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.44 ns | 0.205 ns | 0.172 ns |  5.79 |    0.06 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.74 ns | 0.487 ns | 0.432 ns |  6.66 |    0.08 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.62 ns | 0.135 ns | 0.120 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.16 ns | 0.059 ns | 0.052 ns |  1.15 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 15.81 ns | 0.023 ns | 0.031 ns |  1.38 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 18.31 ns | 0.171 ns | 0.256 ns |  1.60 |    0.03 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.30 ns | 0.066 ns | 0.099 ns |  1.16 |    0.01 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 14.49 ns | 0.073 ns | 0.107 ns |  1.26 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 60.16 ns | 0.405 ns | 0.568 ns |  5.25 |    0.07 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 70.28 ns | 0.531 ns | 0.795 ns |  6.13 |    0.09 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.47 ns | 0.080 ns | 0.119 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.42 ns | 0.011 ns | 0.016 ns |  1.08 |    0.01 |         - |          NA |
