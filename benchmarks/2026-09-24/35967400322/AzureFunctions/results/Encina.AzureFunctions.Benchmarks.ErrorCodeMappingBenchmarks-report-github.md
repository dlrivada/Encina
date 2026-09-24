```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 10.737 ns |  0.2326 ns | 0.3336 ns | 10.525 ns |  1.40 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 11.892 ns |  0.1900 ns | 0.1777 ns | 11.944 ns |  1.55 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  8.461 ns |  0.1847 ns | 0.1728 ns |  8.340 ns |  1.10 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  9.305 ns |  0.1828 ns | 0.1527 ns |  9.250 ns |  1.21 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 39.367 ns |  0.8080 ns | 1.1843 ns | 39.132 ns |  5.13 |    0.16 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 47.195 ns |  0.3009 ns | 0.2512 ns | 47.152 ns |  6.16 |    0.08 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  7.668 ns |  0.1180 ns | 0.0985 ns |  7.661 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.431 ns |  0.1410 ns | 0.1319 ns |  8.405 ns |  1.10 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 11.182 ns |  8.2284 ns | 0.4510 ns | 11.109 ns |  1.39 |    0.08 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 11.977 ns |  0.6487 ns | 0.0356 ns | 11.963 ns |  1.49 |    0.06 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  9.159 ns | 10.8511 ns | 0.5948 ns |  9.254 ns |  1.14 |    0.08 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  9.645 ns |  5.0551 ns | 0.2771 ns |  9.540 ns |  1.20 |    0.06 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 40.455 ns | 36.8006 ns | 2.0172 ns | 39.333 ns |  5.04 |    0.31 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 47.186 ns | 16.8035 ns | 0.9211 ns | 46.764 ns |  5.88 |    0.27 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.040 ns |  7.1832 ns | 0.3937 ns |  8.193 ns |  1.00 |    0.06 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.378 ns |  2.7837 ns | 0.1526 ns |  9.449 ns |  1.17 |    0.05 |         - |          NA |
