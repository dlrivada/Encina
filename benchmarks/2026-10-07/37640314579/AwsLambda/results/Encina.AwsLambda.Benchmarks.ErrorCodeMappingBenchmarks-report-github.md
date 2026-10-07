```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.55 ns | 0.131 ns | 0.116 ns |  1.43 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 14.43 ns | 0.179 ns | 0.168 ns |  1.42 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 11.25 ns | 0.153 ns | 0.135 ns |  1.11 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 12.53 ns | 0.126 ns | 0.112 ns |  1.24 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 12.99 ns | 0.082 ns | 0.077 ns |  1.28 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.05 ns | 0.298 ns | 0.279 ns |  5.53 |    0.14 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 66.83 ns | 0.913 ns | 0.854 ns |  6.59 |    0.18 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.15 ns | 0.240 ns | 0.257 ns |  1.00 |    0.04 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.76 ns | 1.343 ns | 0.074 ns |  1.42 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 15.46 ns | 0.517 ns | 0.028 ns |  1.49 |    0.04 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 11.71 ns | 3.340 ns | 0.183 ns |  1.13 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 12.86 ns | 2.065 ns | 0.113 ns |  1.24 |    0.04 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 13.50 ns | 3.765 ns | 0.206 ns |  1.30 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 53.11 ns | 6.980 ns | 0.383 ns |  5.11 |    0.14 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 64.26 ns | 7.189 ns | 0.394 ns |  6.19 |    0.17 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.39 ns | 6.090 ns | 0.334 ns |  1.00 |    0.04 |         - |          NA |
