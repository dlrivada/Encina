```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.04 ns | 0.142 ns | 0.126 ns |  1.42 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 17.08 ns | 0.210 ns | 0.197 ns |  1.61 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.17 ns | 0.006 ns | 0.006 ns |  1.15 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.79 ns | 0.120 ns | 0.106 ns |  1.30 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.97 ns | 0.018 ns | 0.014 ns |  1.41 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.11 ns | 0.073 ns | 0.057 ns |  5.29 |    0.03 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.93 ns | 0.503 ns | 0.471 ns |  6.68 |    0.06 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.61 ns | 0.080 ns | 0.071 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.92 ns | 2.241 ns | 0.123 ns |  1.42 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.65 ns | 2.711 ns | 0.149 ns |  1.68 |    0.03 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.40 ns | 0.151 ns | 0.008 ns |  1.18 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.41 ns | 0.335 ns | 0.018 ns |  1.28 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.66 ns | 0.282 ns | 0.015 ns |  1.40 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 57.19 ns | 1.352 ns | 0.074 ns |  5.45 |    0.09 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 70.93 ns | 1.089 ns | 0.060 ns |  6.76 |    0.11 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.49 ns | 3.658 ns | 0.201 ns |  1.00 |    0.02 |         - |          NA |
