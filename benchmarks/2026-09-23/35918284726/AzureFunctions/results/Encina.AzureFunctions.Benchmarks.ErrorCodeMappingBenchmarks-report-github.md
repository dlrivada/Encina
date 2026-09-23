```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.96 ns | 0.019 ns | 0.015 ns |  1.42 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.96 ns | 0.010 ns | 0.009 ns |  1.61 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.73 ns | 0.013 ns | 0.011 ns |  1.30 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.99 ns | 0.011 ns | 0.011 ns |  1.42 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.19 ns | 0.115 ns | 0.096 ns |  5.32 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.70 ns | 0.052 ns | 0.043 ns |  6.70 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.56 ns | 0.011 ns | 0.009 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.18 ns | 0.010 ns | 0.008 ns |  1.15 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.94 ns | 4.756 ns | 0.261 ns |  1.44 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.36 ns | 0.275 ns | 0.015 ns |  1.58 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.42 ns | 0.129 ns | 0.007 ns |  1.30 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.69 ns | 0.383 ns | 0.021 ns |  1.42 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.59 ns | 3.845 ns | 0.211 ns |  5.38 |    0.02 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 71.03 ns | 0.309 ns | 0.017 ns |  6.87 |    0.00 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.34 ns | 0.042 ns | 0.002 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.45 ns | 0.087 ns | 0.005 ns |  1.20 |    0.00 |         - |          NA |
