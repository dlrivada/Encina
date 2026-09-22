```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.475 ns |  0.0680 ns | 0.0603 ns |  1.45 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.341 ns |  0.1847 ns | 0.1728 ns |  1.44 |    0.04 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.494 ns |  0.0956 ns | 0.0848 ns |  1.22 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.144 ns |  0.1760 ns | 0.1647 ns |  1.30 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 47.977 ns |  0.3016 ns | 0.2821 ns |  5.59 |    0.13 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.538 ns |  0.3344 ns | 0.3128 ns |  6.59 |    0.15 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.584 ns |  0.2040 ns | 0.1908 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.488 ns |  0.1203 ns | 0.1067 ns |  1.11 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.536 ns |  0.6235 ns | 0.0342 ns |  1.45 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.328 ns |  3.3543 ns | 0.1839 ns |  1.54 |    0.05 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 11.363 ns |  3.4748 ns | 0.1905 ns |  1.31 |    0.04 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.331 ns |  2.3711 ns | 0.1300 ns |  1.31 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.483 ns |  5.6454 ns | 0.3094 ns |  5.37 |    0.16 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 56.832 ns | 10.8889 ns | 0.5969 ns |  6.57 |    0.20 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.663 ns |  5.4261 ns | 0.2974 ns |  1.00 |    0.04 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.623 ns |  0.4370 ns | 0.0240 ns |  1.11 |    0.03 |         - |          NA |
