```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.76 ns | 0.106 ns | 0.089 ns |  1.48 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.93 ns | 0.038 ns | 0.032 ns |  1.67 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.88 ns | 0.008 ns | 0.007 ns |  1.22 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.50 ns | 0.014 ns | 0.012 ns |  1.37 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 65.96 ns | 0.587 ns | 0.549 ns |  5.82 |    0.05 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 73.66 ns | 0.051 ns | 0.048 ns |  6.50 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.34 ns | 0.024 ns | 0.020 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.68 ns | 0.009 ns | 0.008 ns |  1.12 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 16.67 ns | 0.114 ns | 0.156 ns |  1.44 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 18.91 ns | 0.017 ns | 0.022 ns |  1.63 |    0.00 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.52 ns | 0.108 ns | 0.155 ns |  1.17 |    0.01 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 15.71 ns | 0.021 ns | 0.031 ns |  1.36 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 64.41 ns | 0.387 ns | 0.579 ns |  5.56 |    0.05 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 73.97 ns | 0.077 ns | 0.111 ns |  6.39 |    0.01 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.58 ns | 0.007 ns | 0.009 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.75 ns | 0.006 ns | 0.008 ns |  1.10 |    0.00 |         - |          NA |
