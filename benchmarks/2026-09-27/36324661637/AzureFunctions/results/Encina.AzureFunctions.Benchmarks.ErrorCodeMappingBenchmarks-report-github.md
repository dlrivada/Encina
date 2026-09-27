```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.00 ns | 0.054 ns | 0.048 ns |  1.41 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.96 ns | 0.020 ns | 0.018 ns |  1.59 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.73 ns | 0.011 ns | 0.009 ns |  1.29 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.98 ns | 0.011 ns | 0.009 ns |  1.40 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.14 ns | 0.034 ns | 0.026 ns |  5.26 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.70 ns | 0.032 ns | 0.027 ns |  6.63 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.66 ns | 0.012 ns | 0.011 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.19 ns | 0.030 ns | 0.025 ns |  1.14 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.86 ns | 1.470 ns | 0.081 ns |  1.41 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.46 ns | 0.099 ns | 0.005 ns |  1.57 |    0.04 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.41 ns | 0.098 ns | 0.005 ns |  1.28 |    0.03 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.71 ns | 0.870 ns | 0.048 ns |  1.40 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.40 ns | 0.429 ns | 0.024 ns |  5.27 |    0.12 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 70.93 ns | 0.045 ns | 0.002 ns |  6.75 |    0.16 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.51 ns | 5.269 ns | 0.289 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.40 ns | 0.129 ns | 0.007 ns |  1.18 |    0.03 |         - |          NA |
