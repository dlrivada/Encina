```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.00 ns | 0.088 ns | 0.082 ns | 14.97 ns |  1.41 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.93 ns | 0.014 ns | 0.011 ns | 16.93 ns |  1.59 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.22 ns | 0.086 ns | 0.080 ns | 12.17 ns |  1.15 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.76 ns | 0.090 ns | 0.079 ns | 13.72 ns |  1.30 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.97 ns | 0.017 ns | 0.015 ns | 14.97 ns |  1.41 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.30 ns | 0.225 ns | 0.200 ns | 56.32 ns |  5.30 |    0.04 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.66 ns | 0.059 ns | 0.049 ns | 70.67 ns |  6.66 |    0.04 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.62 ns | 0.067 ns | 0.063 ns | 10.61 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 14.85 ns | 0.087 ns | 0.127 ns | 14.81 ns |  1.43 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 16.36 ns | 0.020 ns | 0.029 ns | 16.35 ns |  1.57 |    0.01 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.46 ns | 0.054 ns | 0.081 ns | 12.41 ns |  1.20 |    0.01 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.42 ns | 0.013 ns | 0.019 ns | 13.41 ns |  1.29 |    0.01 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 14.85 ns | 0.146 ns | 0.189 ns | 14.84 ns |  1.43 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 55.81 ns | 0.231 ns | 0.346 ns | 55.82 ns |  5.37 |    0.04 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 70.97 ns | 0.117 ns | 0.160 ns | 70.92 ns |  6.82 |    0.04 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 10.40 ns | 0.045 ns | 0.060 ns | 10.44 ns |  1.00 |    0.01 |         - |          NA |
