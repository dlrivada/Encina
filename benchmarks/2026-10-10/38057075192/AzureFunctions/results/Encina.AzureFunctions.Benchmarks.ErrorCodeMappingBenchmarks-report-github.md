```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.559 ns | 0.1214 ns | 0.1136 ns |  1.45 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  9.668 ns | 0.1141 ns | 0.1011 ns |  1.64 |    0.04 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.898 ns | 0.1533 ns | 0.1359 ns |  1.17 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.722 ns | 0.1014 ns | 0.0899 ns |  1.31 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 31.365 ns | 0.5713 ns | 0.5344 ns |  5.31 |    0.14 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 37.782 ns | 0.4743 ns | 0.4205 ns |  6.40 |    0.15 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.905 ns | 0.1364 ns | 0.1340 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.303 ns | 0.0370 ns | 0.0309 ns |  1.07 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.563 ns | 4.4169 ns | 0.2421 ns |  1.45 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  9.623 ns | 3.0585 ns | 0.1676 ns |  1.63 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.778 ns | 2.6012 ns | 0.1426 ns |  1.15 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.145 ns | 1.5438 ns | 0.0846 ns |  1.38 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 31.096 ns | 3.8808 ns | 0.2127 ns |  5.28 |    0.04 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 37.837 ns | 4.3138 ns | 0.2365 ns |  6.42 |    0.05 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.894 ns | 0.7318 ns | 0.0401 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.282 ns | 0.0907 ns | 0.0050 ns |  1.07 |    0.01 |         - |          NA |
