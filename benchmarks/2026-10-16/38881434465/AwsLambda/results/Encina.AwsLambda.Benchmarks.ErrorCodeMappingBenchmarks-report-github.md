```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.159 ns | 0.2009 ns | 0.2541 ns |  1.53 |    0.06 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.083 ns | 0.0731 ns | 0.0610 ns |  1.68 |    0.05 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.266 ns | 0.0759 ns | 0.0710 ns |  1.05 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.569 ns | 0.1517 ns | 0.2223 ns |  1.10 |    0.05 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  8.102 ns | 0.0284 ns | 0.0252 ns |  1.35 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 33.064 ns | 0.1957 ns | 0.1831 ns |  5.51 |    0.17 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 37.799 ns | 0.3130 ns | 0.2613 ns |  6.30 |    0.19 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.001 ns | 0.1396 ns | 0.1815 ns |  1.00 |    0.04 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.023 ns | 5.2901 ns | 0.2900 ns |  1.52 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.164 ns | 2.7029 ns | 0.1482 ns |  1.71 |    0.03 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.285 ns | 0.4768 ns | 0.0261 ns |  1.06 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.549 ns | 0.8197 ns | 0.0449 ns |  1.10 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.520 ns | 0.1109 ns | 0.0061 ns |  1.27 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 33.018 ns | 2.7363 ns | 0.1500 ns |  5.57 |    0.05 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 38.285 ns | 1.5116 ns | 0.0829 ns |  6.46 |    0.06 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.930 ns | 1.1191 ns | 0.0613 ns |  1.00 |    0.01 |         - |          NA |
