```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.36GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.59 ns | 0.080 ns | 0.071 ns |  1.34 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.94 ns | 0.025 ns | 0.021 ns |  1.46 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.39 ns | 0.327 ns | 0.581 ns |  1.24 |    0.05 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.31 ns | 0.037 ns | 0.031 ns |  1.23 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 60.26 ns | 0.074 ns | 0.066 ns |  5.19 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.24 ns | 0.266 ns | 0.208 ns |  6.05 |    0.03 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.61 ns | 0.043 ns | 0.040 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.29 ns | 0.032 ns | 0.028 ns |  1.06 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 15.69 ns | 0.064 ns | 0.087 ns |  1.35 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 17.10 ns | 0.015 ns | 0.021 ns |  1.47 |    0.00 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.57 ns | 0.130 ns | 0.183 ns |  1.17 |    0.02 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 14.72 ns | 0.054 ns | 0.078 ns |  1.27 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 60.17 ns | 0.146 ns | 0.199 ns |  5.18 |    0.02 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 71.45 ns | 0.643 ns | 0.923 ns |  6.15 |    0.08 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.63 ns | 0.025 ns | 0.037 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 13.36 ns | 0.109 ns | 0.159 ns |  1.15 |    0.01 |         - |          NA |
