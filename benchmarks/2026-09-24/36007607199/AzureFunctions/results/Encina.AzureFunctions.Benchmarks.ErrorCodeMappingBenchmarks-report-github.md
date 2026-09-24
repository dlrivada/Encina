```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 10.917 ns |  0.2451 ns | 0.5431 ns | 10.595 ns |  1.45 |    0.07 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 11.749 ns |  0.0356 ns | 0.0333 ns | 11.750 ns |  1.56 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  8.535 ns |  0.1867 ns | 0.2617 ns |  8.416 ns |  1.13 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  9.272 ns |  0.0937 ns | 0.0732 ns |  9.248 ns |  1.23 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 38.990 ns |  0.7756 ns | 1.1610 ns | 38.490 ns |  5.17 |    0.15 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 47.995 ns |  0.9583 ns | 1.5474 ns | 47.199 ns |  6.36 |    0.20 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  7.547 ns |  0.0228 ns | 0.0178 ns |  7.555 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.408 ns |  0.1930 ns | 0.3170 ns |  8.242 ns |  1.11 |    0.04 |         - |          NA |
|                                 |            |                |             |             |           |            |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 10.667 ns |  2.1778 ns | 0.1194 ns | 10.631 ns |  1.27 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 11.911 ns |  4.6340 ns | 0.2540 ns | 11.817 ns |  1.42 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  8.826 ns |  6.6032 ns | 0.3619 ns |  8.642 ns |  1.05 |    0.04 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  9.462 ns |  3.5729 ns | 0.1958 ns |  9.357 ns |  1.13 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 38.151 ns |  0.3470 ns | 0.0190 ns | 38.149 ns |  4.55 |    0.02 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 47.276 ns | 13.7520 ns | 0.7538 ns | 46.933 ns |  5.64 |    0.08 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.377 ns |  0.8990 ns | 0.0493 ns |  8.361 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  8.454 ns |  2.5312 ns | 0.1387 ns |  8.380 ns |  1.01 |    0.02 |         - |          NA |
