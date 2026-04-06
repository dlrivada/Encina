```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.16 ns |  0.023 ns | 0.020 ns |  1.00 |    0.00 |         - |          NA |
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.83 ns |  0.007 ns | 0.006 ns |  1.42 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.32 ns |  0.016 ns | 0.014 ns |  1.64 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.18 ns |  0.024 ns | 0.021 ns |  1.09 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.73 ns |  0.010 ns | 0.009 ns |  1.23 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.98 ns |  0.017 ns | 0.013 ns |  1.34 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.58 ns |  0.044 ns | 0.036 ns |  5.52 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.70 ns |  0.161 ns | 0.134 ns |  6.34 |    0.02 |         - |          NA |
|                                 |            |                |             |             |          |           |          |       |         |           |             |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.44 ns |  0.026 ns | 0.001 ns |  1.00 |    0.00 |         - |          NA |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.81 ns |  0.174 ns | 0.010 ns |  1.38 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 18.33 ns |  1.044 ns | 0.057 ns |  1.60 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.56 ns |  0.170 ns | 0.009 ns |  1.10 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.39 ns |  0.105 ns | 0.006 ns |  1.17 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.75 ns |  2.688 ns | 0.147 ns |  1.29 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 62.14 ns | 29.366 ns | 1.610 ns |  5.43 |    0.12 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 71.99 ns | 35.527 ns | 1.947 ns |  6.29 |    0.15 |         - |          NA |
