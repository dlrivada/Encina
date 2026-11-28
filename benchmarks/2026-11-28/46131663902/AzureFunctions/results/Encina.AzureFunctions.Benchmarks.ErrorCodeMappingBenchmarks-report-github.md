```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.82GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 11.022 ns |  0.2149 ns | 0.2640 ns |  1.38 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.421 ns |  0.2497 ns | 0.3159 ns |  1.56 |    0.05 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  8.811 ns |  0.1948 ns | 0.1822 ns |  1.11 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  9.518 ns |  0.1246 ns | 0.1104 ns |  1.20 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 40.197 ns |  0.6377 ns | 0.6549 ns |  5.05 |    0.13 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 49.346 ns |  0.9958 ns | 1.5794 ns |  6.20 |    0.23 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  7.967 ns |  0.1806 ns | 0.1689 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.373 ns |  0.0487 ns | 0.0380 ns |  1.05 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 10.822 ns |  0.6935 ns | 0.0380 ns |  1.24 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 12.686 ns | 11.1067 ns | 0.6088 ns |  1.45 |    0.07 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  8.862 ns |  1.3016 ns | 0.0713 ns |  1.01 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  9.683 ns |  1.9528 ns | 0.1070 ns |  1.11 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 39.851 ns | 12.0436 ns | 0.6601 ns |  4.56 |    0.11 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 51.729 ns | 12.6758 ns | 0.6948 ns |  5.92 |    0.14 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.736 ns |  3.6378 ns | 0.1994 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  8.685 ns |  1.4429 ns | 0.0791 ns |  0.99 |    0.02 |         - |          NA |
