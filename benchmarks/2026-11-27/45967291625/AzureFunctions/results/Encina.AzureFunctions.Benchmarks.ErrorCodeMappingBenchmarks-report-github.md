```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 10.898 ns |  0.1482 ns | 0.1238 ns | 10.849 ns |  1.40 |    0.06 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.646 ns |  0.2750 ns | 0.4594 ns | 12.540 ns |  1.63 |    0.09 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  8.976 ns |  0.2042 ns | 0.3179 ns |  8.941 ns |  1.15 |    0.06 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  9.507 ns |  0.1029 ns | 0.0803 ns |  9.476 ns |  1.22 |    0.05 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 39.720 ns |  0.3370 ns | 0.2988 ns | 39.558 ns |  5.11 |    0.21 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 49.343 ns |  1.0134 ns | 1.6365 ns | 48.444 ns |  6.35 |    0.33 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  7.787 ns |  0.1764 ns | 0.3398 ns |  7.629 ns |  1.00 |    0.06 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.204 ns |  0.0759 ns | 0.0634 ns |  8.173 ns |  1.06 |    0.04 |         - |          NA |
|                                 |            |                |             |             |           |            |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 11.145 ns |  8.9124 ns | 0.4885 ns | 10.866 ns |  1.41 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.417 ns | 14.1240 ns | 0.7742 ns | 13.397 ns |  1.70 |    0.09 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  9.493 ns |  0.6058 ns | 0.0332 ns |  9.485 ns |  1.21 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  9.617 ns |  3.2027 ns | 0.1755 ns |  9.547 ns |  1.22 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 40.051 ns |  9.2056 ns | 0.5046 ns | 39.770 ns |  5.08 |    0.06 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 45.890 ns |  2.1435 ns | 0.1175 ns | 45.826 ns |  5.83 |    0.01 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  7.878 ns |  0.1626 ns | 0.0089 ns |  7.882 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  8.490 ns |  1.1130 ns | 0.0610 ns |  8.483 ns |  1.08 |    0.01 |         - |          NA |
