```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.84 ns | 0.026 ns | 0.022 ns | 15.84 ns |  1.42 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.33 ns | 0.012 ns | 0.009 ns | 18.33 ns |  1.64 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.17 ns | 0.006 ns | 0.005 ns | 12.17 ns |  1.09 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.73 ns | 0.010 ns | 0.009 ns | 13.72 ns |  1.23 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.98 ns | 0.016 ns | 0.013 ns | 14.98 ns |  1.34 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.69 ns | 0.192 ns | 0.170 ns | 61.64 ns |  5.53 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.62 ns | 0.079 ns | 0.066 ns | 70.63 ns |  6.33 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.15 ns | 0.010 ns | 0.008 ns | 11.15 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 16.14 ns | 0.230 ns | 0.314 ns | 16.37 ns |  1.41 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 18.30 ns | 0.007 ns | 0.010 ns | 18.30 ns |  1.60 |    0.00 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.42 ns | 0.023 ns | 0.031 ns | 12.41 ns |  1.08 |    0.00 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.42 ns | 0.010 ns | 0.013 ns | 13.42 ns |  1.17 |    0.00 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 14.68 ns | 0.013 ns | 0.017 ns | 14.68 ns |  1.28 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 60.37 ns | 0.035 ns | 0.051 ns | 60.37 ns |  5.27 |    0.01 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 70.94 ns | 0.038 ns | 0.055 ns | 70.93 ns |  6.20 |    0.01 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.45 ns | 0.005 ns | 0.006 ns | 11.45 ns |  1.00 |    0.00 |         - |          NA |
