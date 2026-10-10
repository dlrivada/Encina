```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.95 ns | 0.037 ns | 0.029 ns |  1.42 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.95 ns | 0.008 ns | 0.007 ns |  1.61 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.73 ns | 0.013 ns | 0.011 ns |  1.30 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.97 ns | 0.009 ns | 0.009 ns |  1.42 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 62.54 ns | 0.421 ns | 0.394 ns |  5.93 |    0.04 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.60 ns | 0.048 ns | 0.044 ns |  6.69 |    0.00 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.55 ns | 0.005 ns | 0.004 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.18 ns | 0.021 ns | 0.018 ns |  1.15 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.79 ns | 0.137 ns | 0.007 ns |  1.43 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.38 ns | 0.588 ns | 0.032 ns |  1.59 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.45 ns | 1.221 ns | 0.067 ns |  1.30 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.68 ns | 0.690 ns | 0.038 ns |  1.42 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.45 ns | 0.489 ns | 0.027 ns |  5.37 |    0.00 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 71.26 ns | 7.259 ns | 0.398 ns |  6.90 |    0.03 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.32 ns | 0.067 ns | 0.004 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.40 ns | 0.124 ns | 0.007 ns |  1.20 |    0.00 |         - |          NA |
