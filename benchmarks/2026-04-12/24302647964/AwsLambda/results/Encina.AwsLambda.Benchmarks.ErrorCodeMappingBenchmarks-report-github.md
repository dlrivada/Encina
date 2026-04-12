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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.84 ns | 0.017 ns | 0.013 ns | 15.84 ns |  1.42 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.32 ns | 0.012 ns | 0.010 ns | 18.32 ns |  1.64 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.19 ns | 0.043 ns | 0.036 ns | 12.17 ns |  1.09 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.73 ns | 0.012 ns | 0.010 ns | 13.73 ns |  1.23 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.96 ns | 0.010 ns | 0.009 ns | 14.97 ns |  1.34 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.60 ns | 0.049 ns | 0.041 ns | 61.58 ns |  5.51 |    0.03 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.66 ns | 0.061 ns | 0.048 ns | 70.65 ns |  6.32 |    0.03 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.18 ns | 0.069 ns | 0.058 ns | 11.15 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 15.82 ns | 0.009 ns | 0.013 ns | 15.82 ns |  1.38 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 18.31 ns | 0.008 ns | 0.011 ns | 18.31 ns |  1.59 |    0.00 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.27 ns | 0.104 ns | 0.146 ns | 12.39 ns |  1.07 |    0.01 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.41 ns | 0.007 ns | 0.010 ns | 13.41 ns |  1.17 |    0.00 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 14.67 ns | 0.008 ns | 0.011 ns | 14.67 ns |  1.28 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 60.45 ns | 0.085 ns | 0.116 ns | 60.41 ns |  5.26 |    0.02 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 70.92 ns | 0.106 ns | 0.149 ns | 70.93 ns |  6.17 |    0.02 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.49 ns | 0.023 ns | 0.032 ns | 11.49 ns |  1.00 |    0.00 |         - |          NA |
