```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.47GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.40 ns | 0.045 ns | 0.038 ns | 15.38 ns |  1.33 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.85 ns | 0.019 ns | 0.015 ns | 16.85 ns |  1.45 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 12.97 ns | 0.031 ns | 0.028 ns | 12.97 ns |  1.12 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.06 ns | 0.195 ns | 0.182 ns | 13.99 ns |  1.21 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 60.00 ns | 0.110 ns | 0.092 ns | 59.97 ns |  5.17 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 69.20 ns | 0.204 ns | 0.170 ns | 69.24 ns |  5.96 |    0.02 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.61 ns | 0.016 ns | 0.015 ns | 11.61 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.68 ns | 0.062 ns | 0.055 ns | 12.68 ns |  1.09 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 15.09 ns | 0.016 ns | 0.022 ns | 15.09 ns |  1.33 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 16.78 ns | 0.011 ns | 0.015 ns | 16.78 ns |  1.47 |    0.01 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.91 ns | 0.605 ns | 0.848 ns | 13.26 ns |  1.22 |    0.07 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 14.73 ns | 0.018 ns | 0.026 ns | 14.73 ns |  1.29 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 59.58 ns | 0.054 ns | 0.075 ns | 59.56 ns |  5.24 |    0.03 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 69.59 ns | 0.137 ns | 0.192 ns | 69.63 ns |  6.12 |    0.03 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.38 ns | 0.040 ns | 0.057 ns | 11.38 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.62 ns | 0.025 ns | 0.037 ns | 12.62 ns |  1.11 |    0.01 |         - |          NA |
