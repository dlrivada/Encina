```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.15 ns |  0.021 ns | 0.018 ns |  1.36 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.17 ns |  0.026 ns | 0.022 ns |  1.54 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.10 ns |  0.006 ns | 0.005 ns |  1.11 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.37 ns |  0.044 ns | 0.039 ns |  1.21 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.90 ns |  0.012 ns | 0.010 ns |  1.34 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.42 ns |  0.039 ns | 0.031 ns |  5.19 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 75.83 ns |  0.181 ns | 0.151 ns |  6.41 |    0.03 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.83 ns |  0.064 ns | 0.057 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |           |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.93 ns |  0.140 ns | 0.008 ns |  1.38 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.93 ns |  0.201 ns | 0.011 ns |  1.55 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.34 ns |  0.253 ns | 0.014 ns |  1.15 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.03 ns |  0.155 ns | 0.009 ns |  1.21 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.63 ns |  1.622 ns | 0.089 ns |  1.35 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.49 ns |  0.394 ns | 0.022 ns |  5.22 |    0.01 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 76.51 ns | 10.745 ns | 0.589 ns |  6.61 |    0.05 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.58 ns |  0.448 ns | 0.025 ns |  1.00 |    0.00 |         - |          NA |
