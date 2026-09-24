```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 10.466 ns | 0.0200 ns | 0.0156 ns |  1.34 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 11.637 ns | 0.0948 ns | 0.0791 ns |  1.49 |    0.05 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.156 ns | 0.0233 ns | 0.0182 ns |  1.04 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  8.531 ns | 0.1946 ns | 0.2082 ns |  1.09 |    0.04 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  9.234 ns | 0.0120 ns | 0.0106 ns |  1.18 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 38.236 ns | 0.0668 ns | 0.0522 ns |  4.89 |    0.16 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 47.474 ns | 0.9563 ns | 0.8946 ns |  6.07 |    0.23 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  7.826 ns | 0.1840 ns | 0.2696 ns |  1.00 |    0.05 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 10.537 ns | 0.0670 ns | 0.0037 ns |  1.39 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 12.210 ns | 5.9940 ns | 0.3285 ns |  1.61 |    0.04 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  8.403 ns | 0.4303 ns | 0.0236 ns |  1.11 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  8.593 ns | 0.2397 ns | 0.0131 ns |  1.13 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  9.330 ns | 0.0733 ns | 0.0040 ns |  1.23 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 38.221 ns | 1.2529 ns | 0.0687 ns |  5.03 |    0.02 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 46.966 ns | 1.7674 ns | 0.0969 ns |  6.19 |    0.02 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  7.592 ns | 0.4601 ns | 0.0252 ns |  1.00 |    0.00 |         - |          NA |
