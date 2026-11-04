```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.09 ns | 0.014 ns | 0.011 ns |  1.31 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.82 ns | 0.012 ns | 0.010 ns |  1.46 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.12 ns | 0.054 ns | 0.045 ns |  1.14 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 13.96 ns | 0.037 ns | 0.033 ns |  1.21 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 59.80 ns | 0.106 ns | 0.094 ns |  5.18 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 69.82 ns | 0.182 ns | 0.152 ns |  6.05 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.55 ns | 0.013 ns | 0.011 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.56 ns | 0.033 ns | 0.031 ns |  1.09 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.11 ns | 0.153 ns | 0.008 ns |  1.31 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.80 ns | 0.319 ns | 0.017 ns |  1.46 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.15 ns | 0.947 ns | 0.052 ns |  1.14 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.72 ns | 0.392 ns | 0.022 ns |  1.28 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.00 ns | 2.081 ns | 0.114 ns |  5.21 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 69.66 ns | 1.140 ns | 0.062 ns |  6.04 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.52 ns | 0.409 ns | 0.022 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.63 ns | 0.992 ns | 0.054 ns |  1.10 |         - |          NA |
