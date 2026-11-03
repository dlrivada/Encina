```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.818 ns | 0.1713 ns | 0.1602 ns |  1.43 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 14.013 ns | 0.1601 ns | 0.1497 ns |  1.56 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.409 ns | 0.0390 ns | 0.0346 ns |  1.16 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.879 ns | 0.0977 ns | 0.0866 ns |  1.32 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 47.461 ns | 0.5677 ns | 0.5310 ns |  5.28 |    0.09 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 58.562 ns | 1.1134 ns | 1.0415 ns |  6.51 |    0.14 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.991 ns | 0.1414 ns | 0.1254 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 10.000 ns | 0.0850 ns | 0.0753 ns |  1.11 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.642 ns | 1.2437 ns | 0.0682 ns |  1.38 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.926 ns | 0.8635 ns | 0.0473 ns |  1.52 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.433 ns | 0.3312 ns | 0.0182 ns |  1.14 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 12.054 ns | 2.7472 ns | 0.1506 ns |  1.31 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 47.282 ns | 3.5273 ns | 0.1933 ns |  5.15 |    0.03 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 58.127 ns | 3.5923 ns | 0.1969 ns |  6.33 |    0.03 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  9.182 ns | 0.6441 ns | 0.0353 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 10.079 ns | 2.8102 ns | 0.1540 ns |  1.10 |    0.01 |         - |          NA |
