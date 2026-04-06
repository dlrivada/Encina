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
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.17 ns | 0.022 ns | 0.017 ns | 11.16 ns |  1.00 |    0.00 |         - |          NA |
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.84 ns | 0.017 ns | 0.015 ns | 15.83 ns |  1.42 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.33 ns | 0.017 ns | 0.014 ns | 18.34 ns |  1.64 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.17 ns | 0.016 ns | 0.014 ns | 12.17 ns |  1.09 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.74 ns | 0.011 ns | 0.011 ns | 13.74 ns |  1.23 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.98 ns | 0.020 ns | 0.018 ns | 14.98 ns |  1.34 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.56 ns | 0.048 ns | 0.040 ns | 61.56 ns |  5.51 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.62 ns | 0.040 ns | 0.036 ns | 70.62 ns |  6.32 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |          |       |         |           |             |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.47 ns | 0.016 ns | 0.021 ns | 11.47 ns |  1.00 |    0.00 |         - |          NA |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 15.84 ns | 0.031 ns | 0.042 ns | 15.82 ns |  1.38 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 18.32 ns | 0.012 ns | 0.016 ns | 18.32 ns |  1.60 |    0.00 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.42 ns | 0.013 ns | 0.018 ns | 12.42 ns |  1.08 |    0.00 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.34 ns | 0.057 ns | 0.076 ns | 13.40 ns |  1.16 |    0.01 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 14.66 ns | 0.010 ns | 0.015 ns | 14.66 ns |  1.28 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 60.46 ns | 0.126 ns | 0.168 ns | 60.40 ns |  5.27 |    0.02 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 70.98 ns | 0.046 ns | 0.064 ns | 70.98 ns |  6.19 |    0.01 |         - |          NA |
