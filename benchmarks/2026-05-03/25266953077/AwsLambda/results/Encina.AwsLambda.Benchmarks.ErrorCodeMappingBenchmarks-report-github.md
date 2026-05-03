```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]     : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.11 ns | 0.119 ns | 0.099 ns |  1.43 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.66 ns | 0.011 ns | 0.010 ns |  1.66 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.69 ns | 0.012 ns | 0.011 ns |  1.13 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.39 ns | 0.028 ns | 0.025 ns |  1.19 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.85 ns | 0.016 ns | 0.015 ns |  1.41 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 66.32 ns | 0.419 ns | 0.392 ns |  5.88 |    0.03 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 75.41 ns | 0.050 ns | 0.042 ns |  6.69 |    0.00 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.27 ns | 0.004 ns | 0.003 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 16.29 ns | 0.005 ns | 0.007 ns |  1.41 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 18.92 ns | 0.016 ns | 0.022 ns |  1.63 |    0.00 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.75 ns | 0.007 ns | 0.010 ns |  1.10 |    0.00 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.37 ns | 0.013 ns | 0.020 ns |  1.15 |    0.00 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 15.69 ns | 0.010 ns | 0.014 ns |  1.35 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 64.92 ns | 0.336 ns | 0.482 ns |  5.61 |    0.04 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 74.22 ns | 0.149 ns | 0.209 ns |  6.41 |    0.02 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.58 ns | 0.009 ns | 0.013 ns |  1.00 |    0.00 |         - |          NA |
