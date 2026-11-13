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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.466 ns | 0.0436 ns | 0.0364 ns |  1.37 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.777 ns | 0.0164 ns | 0.0128 ns |  1.51 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.384 ns | 0.0403 ns | 0.0357 ns |  1.14 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.748 ns | 0.0287 ns | 0.0224 ns |  1.29 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.881 ns | 0.1270 ns | 0.1125 ns |  5.14 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 57.165 ns | 0.2437 ns | 0.2280 ns |  6.27 |    0.02 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  9.124 ns | 0.0091 ns | 0.0071 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.832 ns | 0.0516 ns | 0.0458 ns |  1.08 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.713 ns | 6.5079 ns | 0.3567 ns |  1.47 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.997 ns | 8.5557 ns | 0.4690 ns |  1.61 |    0.05 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.416 ns | 0.7943 ns | 0.0435 ns |  1.20 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.990 ns | 2.2896 ns | 0.1255 ns |  1.38 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 47.047 ns | 3.5964 ns | 0.1971 ns |  5.42 |    0.04 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.365 ns | 0.8927 ns | 0.0489 ns |  6.61 |    0.04 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.673 ns | 1.0513 ns | 0.0576 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.846 ns | 0.9278 ns | 0.0509 ns |  1.14 |    0.01 |         - |          NA |
