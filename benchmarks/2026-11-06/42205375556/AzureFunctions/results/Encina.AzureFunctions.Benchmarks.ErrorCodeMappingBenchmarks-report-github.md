```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.19 ns | 0.052 ns | 0.044 ns |  1.38 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.15 ns | 0.013 ns | 0.011 ns |  1.54 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.35 ns | 0.011 ns | 0.010 ns |  1.22 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.90 ns | 0.009 ns | 0.008 ns |  1.35 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.46 ns | 0.215 ns | 0.180 ns |  5.22 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 76.80 ns | 0.087 ns | 0.082 ns |  6.52 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.77 ns | 0.005 ns | 0.004 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.11 ns | 0.038 ns | 0.031 ns |  1.11 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.96 ns | 1.134 ns | 0.062 ns |  1.39 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.93 ns | 0.070 ns | 0.004 ns |  1.56 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.05 ns | 0.824 ns | 0.045 ns |  1.22 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.64 ns | 0.798 ns | 0.044 ns |  1.36 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.73 ns | 5.364 ns | 0.294 ns |  5.27 |    0.02 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 76.05 ns | 0.058 ns | 0.003 ns |  6.60 |    0.00 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.53 ns | 0.088 ns | 0.005 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.56 ns | 5.304 ns | 0.291 ns |  1.18 |    0.02 |         - |          NA |
