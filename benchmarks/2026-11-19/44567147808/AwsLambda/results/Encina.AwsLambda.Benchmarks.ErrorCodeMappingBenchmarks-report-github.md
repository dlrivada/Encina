```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.493 ns | 0.0561 ns | 0.0497 ns |  1.44 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.802 ns | 0.0406 ns | 0.0317 ns |  1.59 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.808 ns | 0.0078 ns | 0.0065 ns |  1.13 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.411 ns | 0.0811 ns | 0.0719 ns |  1.20 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.736 ns | 0.0118 ns | 0.0092 ns |  1.35 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.900 ns | 0.1633 ns | 0.1363 ns |  5.41 |    0.03 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 57.077 ns | 0.1937 ns | 0.1717 ns |  6.59 |    0.04 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.667 ns | 0.0614 ns | 0.0512 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.581 ns | 1.8797 ns | 0.1030 ns |  1.46 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.684 ns | 0.2704 ns | 0.0148 ns |  1.59 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.875 ns | 0.0498 ns | 0.0027 ns |  1.14 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.358 ns | 0.0450 ns | 0.0025 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.871 ns | 0.5652 ns | 0.0310 ns |  1.38 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 47.555 ns | 0.0567 ns | 0.0031 ns |  5.51 |    0.00 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.988 ns | 1.3506 ns | 0.0740 ns |  6.72 |    0.01 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.632 ns | 0.0415 ns | 0.0023 ns |  1.00 |    0.00 |         - |          NA |
