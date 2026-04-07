```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.36GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.65 ns | 0.050 ns | 0.045 ns | 11.63 ns |  1.00 |    0.01 |         - |          NA |
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.60 ns | 0.034 ns | 0.027 ns | 15.58 ns |  1.34 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.94 ns | 0.031 ns | 0.028 ns | 16.93 ns |  1.45 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.35 ns | 0.011 ns | 0.010 ns | 12.36 ns |  1.06 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.28 ns | 0.323 ns | 0.483 ns | 14.29 ns |  1.23 |    0.04 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.04 ns | 0.023 ns | 0.020 ns | 15.03 ns |  1.29 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 59.87 ns | 0.124 ns | 0.110 ns | 59.85 ns |  5.14 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 71.11 ns | 1.069 ns | 0.947 ns | 70.94 ns |  6.10 |    0.08 |         - |          NA |
|                                 |            |                |             |             |          |          |          |          |       |         |           |             |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.61 ns | 0.027 ns | 0.038 ns | 11.61 ns |  1.00 |    0.00 |         - |          NA |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 15.67 ns | 0.028 ns | 0.037 ns | 15.66 ns |  1.35 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 17.11 ns | 0.013 ns | 0.018 ns | 17.11 ns |  1.47 |    0.01 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.82 ns | 0.277 ns | 0.397 ns | 13.09 ns |  1.10 |    0.03 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.58 ns | 0.164 ns | 0.234 ns | 13.72 ns |  1.17 |    0.02 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 14.75 ns | 0.007 ns | 0.010 ns | 14.74 ns |  1.27 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 60.33 ns | 0.076 ns | 0.108 ns | 60.31 ns |  5.20 |    0.02 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 71.29 ns | 0.294 ns | 0.412 ns | 71.26 ns |  6.14 |    0.04 |         - |          NA |
