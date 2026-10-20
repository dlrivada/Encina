```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.105 ns | 0.1577 ns | 0.1475 ns |  1.49 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.704 ns | 0.1453 ns | 0.1288 ns |  1.76 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.603 ns | 0.1556 ns | 0.1456 ns |  1.08 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.784 ns | 0.1486 ns | 0.1241 ns |  1.11 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.562 ns | 0.1187 ns | 0.1110 ns |  1.24 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 35.141 ns | 0.7024 ns | 0.6898 ns |  5.76 |    0.15 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 39.692 ns | 0.6736 ns | 0.6301 ns |  6.51 |    0.15 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.099 ns | 0.1203 ns | 0.1067 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.576 ns | 4.8755 ns | 0.2672 ns |  1.52 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.851 ns | 1.2557 ns | 0.0688 ns |  1.72 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.415 ns | 1.4600 ns | 0.0800 ns |  1.02 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.832 ns | 1.6005 ns | 0.0877 ns |  1.09 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.872 ns | 0.7092 ns | 0.0389 ns |  1.25 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 34.870 ns | 2.3195 ns | 0.1271 ns |  5.54 |    0.06 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 40.539 ns | 8.1554 ns | 0.4470 ns |  6.44 |    0.09 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.297 ns | 1.3964 ns | 0.0765 ns |  1.00 |    0.01 |         - |          NA |
