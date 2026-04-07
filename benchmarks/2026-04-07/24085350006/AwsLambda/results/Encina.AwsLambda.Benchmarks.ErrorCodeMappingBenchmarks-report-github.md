```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.40 ns | 0.016 ns | 0.012 ns |  1.47 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.32 ns | 0.009 ns | 0.007 ns |  1.64 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.17 ns | 0.006 ns | 0.005 ns |  1.09 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.73 ns | 0.009 ns | 0.007 ns |  1.23 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.96 ns | 0.012 ns | 0.009 ns |  1.34 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 62.09 ns | 0.727 ns | 0.680 ns |  5.56 |    0.06 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 71.11 ns | 0.732 ns | 0.685 ns |  6.37 |    0.06 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.16 ns | 0.006 ns | 0.005 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.83 ns | 0.170 ns | 0.009 ns |  1.38 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 18.43 ns | 2.655 ns | 0.146 ns |  1.61 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.41 ns | 0.094 ns | 0.005 ns |  1.08 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.42 ns | 0.313 ns | 0.017 ns |  1.17 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.69 ns | 0.761 ns | 0.042 ns |  1.28 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.68 ns | 7.881 ns | 0.432 ns |  5.29 |    0.03 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 70.93 ns | 0.230 ns | 0.013 ns |  6.18 |    0.00 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.48 ns | 0.177 ns | 0.010 ns |  1.00 |    0.00 |         - |          NA |
