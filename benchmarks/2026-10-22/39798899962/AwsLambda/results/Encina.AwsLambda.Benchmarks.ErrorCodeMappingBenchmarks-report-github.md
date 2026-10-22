```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 3.56GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.689 ns | 0.1299 ns | 0.1215 ns |  1.54 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 11.066 ns | 0.1920 ns | 0.1796 ns |  1.76 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.854 ns | 0.1558 ns | 0.1381 ns |  1.09 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.977 ns | 0.0992 ns | 0.0879 ns |  1.11 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.873 ns | 0.0711 ns | 0.0555 ns |  1.25 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 36.540 ns | 0.7168 ns | 0.6705 ns |  5.82 |    0.14 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 41.217 ns | 0.4521 ns | 0.3775 ns |  6.56 |    0.13 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.280 ns | 0.1267 ns | 0.1123 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.688 ns | 0.4489 ns | 0.0246 ns |  1.52 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 11.014 ns | 1.7369 ns | 0.0952 ns |  1.73 |    0.03 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.926 ns | 3.8365 ns | 0.2103 ns |  1.08 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  7.246 ns | 2.6731 ns | 0.1465 ns |  1.13 |    0.03 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.218 ns | 2.3674 ns | 0.1298 ns |  1.29 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 36.247 ns | 8.5703 ns | 0.4698 ns |  5.68 |    0.10 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 41.473 ns | 1.6042 ns | 0.0879 ns |  6.50 |    0.10 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.386 ns | 1.9560 ns | 0.1072 ns |  1.00 |    0.02 |         - |          NA |
