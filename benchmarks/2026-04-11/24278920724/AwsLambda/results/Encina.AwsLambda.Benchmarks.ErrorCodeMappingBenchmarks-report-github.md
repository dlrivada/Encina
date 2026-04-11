```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.71 ns | 0.016 ns | 0.012 ns | 16.71 ns |  1.44 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.93 ns | 0.017 ns | 0.015 ns | 18.93 ns |  1.64 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.69 ns | 0.019 ns | 0.018 ns | 12.70 ns |  1.10 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.91 ns | 0.039 ns | 0.034 ns | 13.91 ns |  1.20 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.52 ns | 0.023 ns | 0.019 ns | 15.52 ns |  1.34 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 65.91 ns | 0.608 ns | 0.569 ns | 65.69 ns |  5.70 |    0.08 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 73.79 ns | 0.328 ns | 0.274 ns | 73.67 ns |  6.38 |    0.08 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.57 ns | 0.188 ns | 0.147 ns | 11.62 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |          |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 16.83 ns | 0.160 ns | 0.219 ns | 16.75 ns |  1.45 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 18.95 ns | 0.068 ns | 0.095 ns | 18.92 ns |  1.63 |    0.01 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.75 ns | 0.004 ns | 0.006 ns | 12.75 ns |  1.10 |    0.00 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.64 ns | 0.145 ns | 0.204 ns | 13.80 ns |  1.18 |    0.02 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 15.73 ns | 0.020 ns | 0.028 ns | 15.72 ns |  1.36 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 64.11 ns | 0.309 ns | 0.444 ns | 63.97 ns |  5.53 |    0.04 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 74.06 ns | 0.059 ns | 0.081 ns | 74.06 ns |  6.39 |    0.02 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.60 ns | 0.020 ns | 0.030 ns | 11.58 ns |  1.00 |    0.00 |         - |          NA |
