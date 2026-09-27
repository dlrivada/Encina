```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.49 ns | 0.118 ns | 0.111 ns |  1.44 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 14.37 ns | 0.149 ns | 0.140 ns |  1.43 |    0.04 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 12.07 ns | 0.100 ns | 0.089 ns |  1.20 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 12.70 ns | 0.101 ns | 0.094 ns |  1.26 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 55.69 ns | 0.317 ns | 0.296 ns |  5.54 |    0.14 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 65.82 ns | 0.540 ns | 0.479 ns |  6.54 |    0.17 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.06 ns | 0.245 ns | 0.252 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 10.93 ns | 0.184 ns | 0.172 ns |  1.09 |    0.03 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 14.54 ns | 0.089 ns | 0.133 ns |  1.45 |    0.06 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 15.26 ns | 0.016 ns | 0.023 ns |  1.52 |    0.06 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 12.63 ns | 0.139 ns | 0.208 ns |  1.26 |    0.06 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 13.02 ns | 0.085 ns | 0.122 ns |  1.30 |    0.06 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 53.60 ns | 0.481 ns | 0.719 ns |  5.34 |    0.24 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 65.99 ns | 0.505 ns | 0.740 ns |  6.57 |    0.29 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 10.06 ns | 0.296 ns | 0.434 ns |  1.00 |    0.06 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 11.19 ns | 0.108 ns | 0.158 ns |  1.11 |    0.05 |         - |          NA |
