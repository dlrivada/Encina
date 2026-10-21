```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.16 ns | 0.043 ns | 0.036 ns |  1.37 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.15 ns | 0.012 ns | 0.010 ns |  1.54 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.34 ns | 0.015 ns | 0.012 ns |  1.22 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.96 ns | 0.031 ns | 0.028 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.38 ns | 0.081 ns | 0.068 ns |  5.21 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 75.80 ns | 0.037 ns | 0.033 ns |  6.43 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.79 ns | 0.016 ns | 0.013 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.12 ns | 0.017 ns | 0.014 ns |  1.11 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.92 ns | 0.446 ns | 0.024 ns |  1.38 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.94 ns | 0.026 ns | 0.001 ns |  1.55 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.08 ns | 0.641 ns | 0.035 ns |  1.22 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.60 ns | 0.546 ns | 0.030 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.48 ns | 0.811 ns | 0.044 ns |  5.24 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 76.11 ns | 1.210 ns | 0.066 ns |  6.60 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.54 ns | 0.144 ns | 0.008 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.34 ns | 0.203 ns | 0.011 ns |  1.16 |         - |          NA |
