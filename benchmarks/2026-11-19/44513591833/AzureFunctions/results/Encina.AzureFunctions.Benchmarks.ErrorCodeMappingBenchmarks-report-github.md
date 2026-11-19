```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.83GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 11.322 ns |  0.2451 ns | 0.3515 ns |  1.41 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.413 ns |  0.2720 ns | 0.3023 ns |  1.55 |    0.05 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  8.830 ns |  0.1991 ns | 0.2446 ns |  1.10 |    0.04 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  9.458 ns |  0.0413 ns | 0.0322 ns |  1.18 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 39.746 ns |  0.1165 ns | 0.1033 ns |  4.95 |    0.09 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 49.622 ns |  0.9327 ns | 0.9980 ns |  6.18 |    0.17 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.036 ns |  0.1720 ns | 0.1525 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.940 ns |  0.2069 ns | 0.3281 ns |  1.11 |    0.05 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 11.154 ns |  2.6502 ns | 0.1453 ns |  1.40 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 12.819 ns |  0.5906 ns | 0.0324 ns |  1.61 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  9.103 ns |  5.9689 ns | 0.3272 ns |  1.14 |    0.04 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  9.805 ns |  0.9035 ns | 0.0495 ns |  1.23 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 40.563 ns | 27.1833 ns | 1.4900 ns |  5.09 |    0.16 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 47.910 ns | 10.3416 ns | 0.5669 ns |  6.01 |    0.07 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  7.976 ns |  0.9705 ns | 0.0532 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  8.670 ns |  2.2493 ns | 0.1233 ns |  1.09 |    0.01 |         - |          NA |
