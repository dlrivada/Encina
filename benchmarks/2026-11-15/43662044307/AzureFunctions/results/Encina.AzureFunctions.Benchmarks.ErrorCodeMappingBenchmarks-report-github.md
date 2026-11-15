```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.05GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.07 ns | 0.136 ns | 0.127 ns | 15.03 ns |  1.43 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 17.03 ns | 0.153 ns | 0.136 ns | 16.96 ns |  1.61 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.72 ns | 0.015 ns | 0.011 ns | 13.72 ns |  1.30 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.09 ns | 0.170 ns | 0.159 ns | 14.99 ns |  1.43 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.31 ns | 0.258 ns | 0.228 ns | 56.20 ns |  5.33 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.66 ns | 0.060 ns | 0.047 ns | 70.66 ns |  6.68 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.57 ns | 0.026 ns | 0.020 ns | 10.57 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.23 ns | 0.083 ns | 0.078 ns | 12.19 ns |  1.16 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 14.86 ns | 0.082 ns | 0.120 ns | 14.82 ns |  1.44 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 16.41 ns | 0.095 ns | 0.130 ns | 16.35 ns |  1.59 |    0.01 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.52 ns | 0.120 ns | 0.172 ns | 13.44 ns |  1.31 |    0.02 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 14.73 ns | 0.083 ns | 0.119 ns | 14.68 ns |  1.42 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 55.51 ns | 0.107 ns | 0.147 ns | 55.45 ns |  5.36 |    0.03 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 71.32 ns | 0.306 ns | 0.459 ns | 71.09 ns |  6.89 |    0.05 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 10.35 ns | 0.032 ns | 0.044 ns | 10.34 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.46 ns | 0.053 ns | 0.079 ns | 12.42 ns |  1.20 |    0.01 |         - |          NA |
