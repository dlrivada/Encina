```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.72GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.63 ns | 0.038 ns | 0.029 ns | 15.63 ns |  1.33 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 17.11 ns | 0.047 ns | 0.044 ns | 17.12 ns |  1.46 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.37 ns | 0.051 ns | 0.045 ns | 13.38 ns |  1.14 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.75 ns | 0.159 ns | 0.141 ns | 13.78 ns |  1.17 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.22 ns | 0.083 ns | 0.073 ns | 14.19 ns |  1.21 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 60.22 ns | 0.136 ns | 0.114 ns | 60.22 ns |  5.13 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.78 ns | 0.747 ns | 0.583 ns | 70.62 ns |  6.03 |    0.05 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.73 ns | 0.043 ns | 0.041 ns | 11.74 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 15.54 ns | 0.015 ns | 0.020 ns | 15.54 ns |  1.35 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 16.95 ns | 0.009 ns | 0.013 ns | 16.96 ns |  1.48 |    0.02 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.66 ns | 0.190 ns | 0.273 ns | 12.46 ns |  1.10 |    0.03 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.50 ns | 0.203 ns | 0.291 ns | 13.36 ns |  1.17 |    0.03 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 14.71 ns | 0.038 ns | 0.058 ns | 14.74 ns |  1.28 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 60.25 ns | 0.168 ns | 0.229 ns | 60.27 ns |  5.24 |    0.07 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 71.87 ns | 0.313 ns | 0.438 ns | 71.70 ns |  6.26 |    0.08 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.49 ns | 0.096 ns | 0.141 ns | 11.37 ns |  1.00 |    0.02 |         - |          NA |
