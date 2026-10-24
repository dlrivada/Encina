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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.21 ns | 0.082 ns | 0.073 ns |  1.38 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.18 ns | 0.010 ns | 0.008 ns |  1.54 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.10 ns | 0.007 ns | 0.006 ns |  1.11 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.36 ns | 0.032 ns | 0.026 ns |  1.22 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.90 ns | 0.009 ns | 0.008 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.39 ns | 0.027 ns | 0.024 ns |  5.21 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 75.90 ns | 0.169 ns | 0.158 ns |  6.44 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.78 ns | 0.011 ns | 0.009 ns |  1.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.94 ns | 0.089 ns | 0.005 ns |  1.38 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.95 ns | 0.378 ns | 0.021 ns |  1.56 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.40 ns | 0.917 ns | 0.050 ns |  1.16 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.07 ns | 0.295 ns | 0.016 ns |  1.22 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.59 ns | 0.093 ns | 0.005 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.51 ns | 0.452 ns | 0.025 ns |  5.24 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 76.19 ns | 2.467 ns | 0.135 ns |  6.60 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.54 ns | 0.047 ns | 0.003 ns |  1.00 |         - |          NA |
