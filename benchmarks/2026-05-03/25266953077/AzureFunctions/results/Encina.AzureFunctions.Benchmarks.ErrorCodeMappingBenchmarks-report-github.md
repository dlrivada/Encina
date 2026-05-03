```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]     : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.28 ns | 0.154 ns | 0.136 ns |  1.38 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 17.81 ns | 0.108 ns | 0.090 ns |  1.51 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.85 ns | 0.048 ns | 0.044 ns |  1.18 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.42 ns | 0.057 ns | 0.053 ns |  1.31 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 62.93 ns | 0.738 ns | 0.690 ns |  5.34 |    0.06 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 71.98 ns | 0.928 ns | 0.868 ns |  6.11 |    0.07 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.78 ns | 0.038 ns | 0.035 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.50 ns | 0.056 ns | 0.053 ns |  1.15 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 17.29 ns | 0.023 ns | 0.035 ns |  1.43 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 19.84 ns | 0.027 ns | 0.039 ns |  1.64 |    0.01 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 14.82 ns | 0.032 ns | 0.046 ns |  1.22 |    0.01 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 16.17 ns | 0.031 ns | 0.046 ns |  1.33 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 62.99 ns | 0.249 ns | 0.341 ns |  5.20 |    0.05 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 72.66 ns | 0.231 ns | 0.332 ns |  5.99 |    0.06 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 12.12 ns | 0.073 ns | 0.105 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 14.52 ns | 0.022 ns | 0.032 ns |  1.20 |    0.01 |         - |          NA |
