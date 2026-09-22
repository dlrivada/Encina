```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 13.025 ns | 0.2760 ns | 0.2581 ns |  1.47 |    0.06 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.759 ns | 0.2087 ns | 0.1630 ns |  1.44 |    0.05 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.931 ns | 0.1864 ns | 0.1744 ns |  1.12 |    0.04 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 11.066 ns | 0.2499 ns | 0.2778 ns |  1.25 |    0.05 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.734 ns | 0.2448 ns | 0.2289 ns |  1.32 |    0.05 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 49.519 ns | 0.8500 ns | 0.7535 ns |  5.59 |    0.20 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 57.341 ns | 0.6273 ns | 0.4897 ns |  6.47 |    0.22 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.867 ns | 0.2175 ns | 0.2977 ns |  1.00 |    0.05 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.120 ns | 8.1805 ns | 0.4484 ns |  1.36 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.478 ns | 2.2421 ns | 0.1229 ns |  1.52 |    0.03 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 10.099 ns | 3.3900 ns | 0.1858 ns |  1.14 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 12.227 ns | 3.5970 ns | 0.1972 ns |  1.38 |    0.03 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.790 ns | 4.1667 ns | 0.2284 ns |  1.33 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 47.809 ns | 8.0559 ns | 0.4416 ns |  5.38 |    0.11 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.298 ns | 2.6163 ns | 0.1434 ns |  6.45 |    0.12 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.882 ns | 3.5856 ns | 0.1965 ns |  1.00 |    0.03 |         - |          NA |
