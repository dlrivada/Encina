```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Median   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.83 ns | 0.024 ns | 0.020 ns | 15.83 ns |  1.42 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.32 ns | 0.014 ns | 0.013 ns | 18.32 ns |  1.64 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.72 ns | 0.016 ns | 0.014 ns | 13.72 ns |  1.23 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.97 ns | 0.011 ns | 0.009 ns | 14.97 ns |  1.34 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.55 ns | 0.041 ns | 0.032 ns | 61.55 ns |  5.52 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.66 ns | 0.056 ns | 0.047 ns | 70.66 ns |  6.34 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.15 ns | 0.006 ns | 0.005 ns | 11.15 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.18 ns | 0.033 ns | 0.027 ns | 12.17 ns |  1.09 |         - |          NA |
|                                 |            |                |             |             |          |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 15.84 ns | 0.025 ns | 0.036 ns | 15.83 ns |  1.38 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 18.35 ns | 0.032 ns | 0.045 ns | 18.38 ns |  1.60 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.41 ns | 0.007 ns | 0.010 ns | 13.41 ns |  1.17 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 14.67 ns | 0.008 ns | 0.012 ns | 14.67 ns |  1.28 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 60.42 ns | 0.065 ns | 0.090 ns | 60.40 ns |  5.28 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 70.97 ns | 0.045 ns | 0.064 ns | 70.96 ns |  6.20 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.45 ns | 0.004 ns | 0.006 ns | 11.45 ns |  1.00 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.42 ns | 0.014 ns | 0.019 ns | 12.41 ns |  1.08 |         - |          NA |
