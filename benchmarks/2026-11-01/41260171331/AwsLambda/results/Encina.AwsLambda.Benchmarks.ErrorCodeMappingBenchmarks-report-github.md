```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.17 ns | 0.014 ns | 0.011 ns |  1.32 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.18 ns | 0.022 ns | 0.017 ns |  1.48 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.12 ns | 0.015 ns | 0.012 ns |  1.07 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.36 ns | 0.009 ns | 0.007 ns |  1.17 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.94 ns | 0.032 ns | 0.027 ns |  1.30 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.46 ns | 0.074 ns | 0.058 ns |  5.00 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 75.89 ns | 0.063 ns | 0.059 ns |  6.18 |    0.02 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 12.29 ns | 0.039 ns | 0.035 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 15.96 ns | 0.014 ns | 0.020 ns |  1.38 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 18.06 ns | 0.156 ns | 0.219 ns |  1.56 |    0.02 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 13.35 ns | 0.012 ns | 0.016 ns |  1.15 |    0.00 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 14.04 ns | 0.006 ns | 0.008 ns |  1.21 |    0.00 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 15.61 ns | 0.016 ns | 0.024 ns |  1.35 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 60.76 ns | 0.133 ns | 0.187 ns |  5.25 |    0.02 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 76.15 ns | 0.054 ns | 0.073 ns |  6.57 |    0.02 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.58 ns | 0.024 ns | 0.034 ns |  1.00 |    0.00 |         - |          NA |
