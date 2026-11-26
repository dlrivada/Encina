```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  7.940 ns | 0.0509 ns | 0.0476 ns |  1.46 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  8.858 ns | 0.1175 ns | 0.0917 ns |  1.63 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.679 ns | 0.0188 ns | 0.0147 ns |  1.23 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.269 ns | 0.1626 ns | 0.1358 ns |  1.34 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 29.032 ns | 0.3810 ns | 0.3377 ns |  5.35 |    0.06 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 35.986 ns | 0.3012 ns | 0.2818 ns |  6.63 |    0.06 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.429 ns | 0.0254 ns | 0.0225 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.129 ns | 0.1450 ns | 0.2577 ns |  1.13 |    0.05 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.036 ns | 2.7597 ns | 0.1513 ns |  1.41 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  8.897 ns | 1.8538 ns | 0.1016 ns |  1.56 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.160 ns | 0.8681 ns | 0.0476 ns |  1.08 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.739 ns | 3.0791 ns | 0.1688 ns |  1.36 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 28.946 ns | 1.6982 ns | 0.0931 ns |  5.09 |    0.01 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 35.492 ns | 7.7887 ns | 0.4269 ns |  6.24 |    0.07 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.687 ns | 0.0302 ns | 0.0017 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  5.889 ns | 3.2538 ns | 0.1784 ns |  1.04 |    0.03 |         - |          NA |
