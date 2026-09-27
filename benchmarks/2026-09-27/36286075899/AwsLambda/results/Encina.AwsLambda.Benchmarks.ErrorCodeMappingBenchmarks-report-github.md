```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.94 ns | 0.021 ns | 0.018 ns | 14.95 ns |  1.42 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.93 ns | 0.013 ns | 0.011 ns | 16.92 ns |  1.60 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.16 ns | 0.008 ns | 0.006 ns | 12.16 ns |  1.15 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.75 ns | 0.039 ns | 0.035 ns | 13.73 ns |  1.30 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.00 ns | 0.033 ns | 0.029 ns | 14.99 ns |  1.42 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.83 ns | 0.054 ns | 0.048 ns | 56.81 ns |  5.38 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.89 ns | 0.268 ns | 0.237 ns | 70.81 ns |  6.72 |    0.02 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.56 ns | 0.007 ns | 0.006 ns | 10.56 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 14.78 ns | 0.007 ns | 0.009 ns | 14.78 ns |  1.41 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 16.42 ns | 0.030 ns | 0.043 ns | 16.41 ns |  1.56 |    0.03 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.41 ns | 0.008 ns | 0.011 ns | 12.40 ns |  1.18 |    0.02 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.42 ns | 0.011 ns | 0.015 ns | 13.42 ns |  1.28 |    0.02 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 14.68 ns | 0.010 ns | 0.013 ns | 14.68 ns |  1.40 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 55.84 ns | 0.309 ns | 0.433 ns | 56.16 ns |  5.31 |    0.10 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 70.93 ns | 0.028 ns | 0.039 ns | 70.93 ns |  6.75 |    0.12 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 10.52 ns | 0.134 ns | 0.184 ns | 10.52 ns |  1.00 |    0.02 |         - |          NA |
