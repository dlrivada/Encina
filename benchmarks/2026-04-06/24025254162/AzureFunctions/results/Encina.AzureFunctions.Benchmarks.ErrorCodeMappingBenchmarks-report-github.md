```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 3.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.16 ns | 0.021 ns | 0.016 ns |  1.00 |    0.00 |         - |          NA |
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.85 ns | 0.035 ns | 0.029 ns |  1.42 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.39 ns | 0.148 ns | 0.123 ns |  1.65 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.17 ns | 0.020 ns | 0.017 ns |  1.09 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.72 ns | 0.015 ns | 0.012 ns |  1.23 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.98 ns | 0.011 ns | 0.010 ns |  1.34 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.77 ns | 0.497 ns | 0.415 ns |  5.53 |    0.04 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.72 ns | 0.147 ns | 0.131 ns |  6.34 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.66 ns | 3.072 ns | 0.168 ns |  1.00 |    0.02 |         - |          NA |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.82 ns | 0.151 ns | 0.008 ns |  1.36 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 18.31 ns | 0.134 ns | 0.007 ns |  1.57 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.43 ns | 0.323 ns | 0.018 ns |  1.07 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.47 ns | 1.541 ns | 0.084 ns |  1.15 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.92 ns | 3.902 ns | 0.214 ns |  1.28 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.92 ns | 8.806 ns | 0.483 ns |  5.22 |    0.07 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 70.93 ns | 0.807 ns | 0.044 ns |  6.08 |    0.08 |         - |          NA |
