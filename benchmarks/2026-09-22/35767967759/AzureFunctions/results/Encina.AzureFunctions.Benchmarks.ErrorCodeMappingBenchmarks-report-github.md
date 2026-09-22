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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  7.933 ns | 0.1012 ns | 0.0845 ns |  1.46 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  9.032 ns | 0.1927 ns | 0.2142 ns |  1.66 |    0.04 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.144 ns | 0.0598 ns | 0.0559 ns |  1.13 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.196 ns | 0.0290 ns | 0.0242 ns |  1.33 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 29.199 ns | 0.5179 ns | 0.4844 ns |  5.38 |    0.09 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 34.924 ns | 0.2189 ns | 0.1940 ns |  6.44 |    0.04 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.426 ns | 0.0201 ns | 0.0168 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  5.696 ns | 0.0764 ns | 0.0677 ns |  1.05 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  7.904 ns | 0.2881 ns | 0.0158 ns |  1.47 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  8.973 ns | 5.7598 ns | 0.3157 ns |  1.67 |    0.05 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.334 ns | 5.6597 ns | 0.3102 ns |  1.18 |    0.05 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.473 ns | 0.5741 ns | 0.0315 ns |  1.39 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 28.846 ns | 3.3500 ns | 0.1836 ns |  5.37 |    0.04 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 35.928 ns | 2.1646 ns | 0.1187 ns |  6.69 |    0.04 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.371 ns | 0.5673 ns | 0.0311 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  5.775 ns | 1.1277 ns | 0.0618 ns |  1.08 |    0.01 |         - |          NA |
