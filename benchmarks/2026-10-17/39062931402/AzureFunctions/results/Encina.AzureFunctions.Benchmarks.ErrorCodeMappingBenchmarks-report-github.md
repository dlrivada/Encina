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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.342 ns |  0.2086 ns | 0.2048 ns |  1.55 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.402 ns |  0.1931 ns | 0.1983 ns |  1.73 |    0.06 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  7.434 ns |  0.1428 ns | 0.1335 ns |  1.24 |    0.04 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.674 ns |  0.1631 ns | 0.2725 ns |  1.28 |    0.06 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 34.372 ns |  0.6784 ns | 0.6663 ns |  5.72 |    0.19 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 39.142 ns |  0.6266 ns | 0.5862 ns |  6.51 |    0.20 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.014 ns |  0.1403 ns | 0.1775 ns |  1.00 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.373 ns |  0.1456 ns | 0.1430 ns |  1.06 |    0.04 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.267 ns |  2.5488 ns | 0.1397 ns |  1.54 |    0.06 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.490 ns |  2.8015 ns | 0.1536 ns |  1.75 |    0.07 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  7.316 ns |  7.3639 ns | 0.4036 ns |  1.22 |    0.07 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.078 ns |  6.7916 ns | 0.3723 ns |  1.34 |    0.07 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 34.238 ns | 17.9488 ns | 0.9838 ns |  5.70 |    0.25 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 40.834 ns | 63.2100 ns | 3.4648 ns |  6.80 |    0.56 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.013 ns |  4.6245 ns | 0.2535 ns |  1.00 |    0.05 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.464 ns |  0.3092 ns | 0.0169 ns |  1.08 |    0.04 |         - |          NA |
