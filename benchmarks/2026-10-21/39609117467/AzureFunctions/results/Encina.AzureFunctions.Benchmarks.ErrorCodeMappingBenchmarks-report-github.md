```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.18 ns |  0.028 ns | 0.024 ns |  1.37 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.22 ns |  0.075 ns | 0.062 ns |  1.55 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.36 ns |  0.015 ns | 0.014 ns |  1.22 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.91 ns |  0.012 ns | 0.010 ns |  1.35 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.44 ns |  0.044 ns | 0.036 ns |  5.21 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 75.86 ns |  0.089 ns | 0.074 ns |  6.43 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.79 ns |  0.022 ns | 0.021 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.13 ns |  0.022 ns | 0.020 ns |  1.11 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |           |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.93 ns |  0.317 ns | 0.017 ns |  1.38 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.98 ns |  0.165 ns | 0.009 ns |  1.56 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.04 ns |  0.090 ns | 0.005 ns |  1.22 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.63 ns |  0.413 ns | 0.023 ns |  1.35 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.61 ns |  0.147 ns | 0.008 ns |  5.25 |    0.00 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 76.56 ns | 14.673 ns | 0.804 ns |  6.63 |    0.06 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.55 ns |  0.196 ns | 0.011 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.48 ns |  3.791 ns | 0.208 ns |  1.17 |    0.02 |         - |          NA |
