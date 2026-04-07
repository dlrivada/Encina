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
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.18 ns | 0.036 ns | 0.032 ns |  1.00 |    0.00 |         - |          NA |
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.84 ns | 0.011 ns | 0.011 ns |  1.42 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 19.19 ns | 0.016 ns | 0.013 ns |  1.72 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.18 ns | 0.011 ns | 0.009 ns |  1.09 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.75 ns | 0.018 ns | 0.014 ns |  1.23 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.99 ns | 0.033 ns | 0.027 ns |  1.34 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.71 ns | 0.193 ns | 0.171 ns |  5.52 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 71.05 ns | 0.132 ns | 0.117 ns |  6.35 |    0.02 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.52 ns | 0.078 ns | 0.111 ns |  1.00 |    0.01 |         - |          NA |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 15.86 ns | 0.012 ns | 0.017 ns |  1.38 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 18.34 ns | 0.040 ns | 0.054 ns |  1.59 |    0.02 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.47 ns | 0.025 ns | 0.035 ns |  1.08 |    0.01 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.43 ns | 0.025 ns | 0.036 ns |  1.17 |    0.01 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 14.68 ns | 0.009 ns | 0.012 ns |  1.27 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 60.55 ns | 0.203 ns | 0.285 ns |  5.25 |    0.05 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 71.08 ns | 0.094 ns | 0.131 ns |  6.17 |    0.06 |         - |          NA |
