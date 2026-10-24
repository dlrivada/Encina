```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.37GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.351 ns |  0.2109 ns | 0.2887 ns |  1.49 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.713 ns |  0.1817 ns | 0.1611 ns |  1.71 |    0.04 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.780 ns |  0.1006 ns | 0.0786 ns |  1.08 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.601 ns |  0.1636 ns | 0.1450 ns |  1.21 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 34.349 ns |  0.6163 ns | 0.5765 ns |  5.49 |    0.14 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 38.922 ns |  0.7688 ns | 0.6816 ns |  6.22 |    0.16 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.258 ns |  0.1226 ns | 0.1259 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.856 ns |  0.1218 ns | 0.1140 ns |  1.10 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.278 ns |  5.6392 ns | 0.3091 ns |  1.51 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.661 ns |  3.0392 ns | 0.1666 ns |  1.74 |    0.04 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.934 ns |  2.6168 ns | 0.1434 ns |  1.13 |    0.03 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.856 ns |  1.1012 ns | 0.0604 ns |  1.28 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 34.976 ns |  6.6926 ns | 0.3668 ns |  5.70 |    0.10 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 40.065 ns | 21.9905 ns | 1.2054 ns |  6.53 |    0.20 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.140 ns |  2.0668 ns | 0.1133 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.622 ns |  3.8543 ns | 0.2113 ns |  1.08 |    0.03 |         - |          NA |
