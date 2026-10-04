```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.97 ns | 0.023 ns | 0.020 ns |  1.42 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.93 ns | 0.015 ns | 0.013 ns |  1.60 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.17 ns | 0.005 ns | 0.004 ns |  1.15 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.74 ns | 0.015 ns | 0.013 ns |  1.30 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.99 ns | 0.034 ns | 0.026 ns |  1.42 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.18 ns | 0.094 ns | 0.083 ns |  5.32 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.71 ns | 0.082 ns | 0.069 ns |  6.69 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.57 ns | 0.021 ns | 0.017 ns |  1.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 14.80 ns | 0.008 ns | 0.010 ns |  1.43 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 16.41 ns | 0.034 ns | 0.048 ns |  1.59 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.41 ns | 0.011 ns | 0.015 ns |  1.20 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.42 ns | 0.011 ns | 0.015 ns |  1.30 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 14.67 ns | 0.011 ns | 0.016 ns |  1.42 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 55.39 ns | 0.041 ns | 0.054 ns |  5.36 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 70.95 ns | 0.048 ns | 0.067 ns |  6.86 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 10.34 ns | 0.008 ns | 0.011 ns |  1.00 |         - |          NA |
