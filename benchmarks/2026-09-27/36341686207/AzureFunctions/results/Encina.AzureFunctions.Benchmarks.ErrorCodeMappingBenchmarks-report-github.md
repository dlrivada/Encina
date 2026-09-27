```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.98 ns | 0.050 ns | 0.044 ns |  1.42 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.95 ns | 0.014 ns | 0.012 ns |  1.61 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.73 ns | 0.015 ns | 0.012 ns |  1.30 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.97 ns | 0.020 ns | 0.017 ns |  1.42 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.17 ns | 0.084 ns | 0.074 ns |  5.33 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.80 ns | 0.179 ns | 0.150 ns |  6.71 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.55 ns | 0.008 ns | 0.006 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.27 ns | 0.144 ns | 0.128 ns |  1.16 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.80 ns | 0.943 ns | 0.052 ns |  1.43 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.36 ns | 0.177 ns | 0.010 ns |  1.58 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.41 ns | 0.111 ns | 0.006 ns |  1.29 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.67 ns | 0.311 ns | 0.017 ns |  1.42 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.40 ns | 1.605 ns | 0.088 ns |  5.35 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 70.93 ns | 0.274 ns | 0.015 ns |  6.85 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.36 ns | 0.290 ns | 0.016 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.42 ns | 0.491 ns | 0.027 ns |  1.20 |         - |          NA |
