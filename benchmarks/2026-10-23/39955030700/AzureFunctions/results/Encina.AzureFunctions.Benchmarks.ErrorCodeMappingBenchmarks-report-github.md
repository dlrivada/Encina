```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 10.450 ns |  0.1783 ns | 0.1668 ns |  1.76 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.971 ns |  0.2072 ns | 0.1938 ns |  1.85 |    0.05 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.890 ns |  0.1148 ns | 0.1074 ns |  1.16 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.615 ns |  0.1674 ns | 0.1719 ns |  1.29 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 33.505 ns |  0.5527 ns | 0.4899 ns |  5.66 |    0.14 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 39.247 ns |  0.5143 ns | 0.4559 ns |  6.63 |    0.16 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.926 ns |  0.1324 ns | 0.1300 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.266 ns |  0.1398 ns | 0.1496 ns |  1.06 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.017 ns |  1.3449 ns | 0.0737 ns |  1.55 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.354 ns |  4.4100 ns | 0.2417 ns |  1.78 |    0.04 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.512 ns |  3.8362 ns | 0.2103 ns |  1.12 |    0.04 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.622 ns |  3.4531 ns | 0.1893 ns |  1.31 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 34.536 ns | 25.8029 ns | 1.4143 ns |  5.95 |    0.23 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 37.930 ns |  7.8943 ns | 0.4327 ns |  6.54 |    0.11 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.802 ns |  1.6739 ns | 0.0918 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.369 ns |  1.5367 ns | 0.0842 ns |  1.10 |    0.02 |         - |          NA |
