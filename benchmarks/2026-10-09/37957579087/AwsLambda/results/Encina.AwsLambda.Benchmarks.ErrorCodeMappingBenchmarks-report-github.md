```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 10.825 ns |  0.2279 ns | 0.4224 ns | 10.704 ns |  1.43 |    0.06 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 11.492 ns |  0.0957 ns | 0.0747 ns | 11.487 ns |  1.52 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.050 ns |  0.1592 ns | 0.1895 ns |  7.968 ns |  1.07 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  8.486 ns |  0.1389 ns | 0.1364 ns |  8.417 ns |  1.12 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  9.788 ns |  0.2517 ns | 0.7421 ns |  9.297 ns |  1.30 |    0.10 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 38.728 ns |  0.2921 ns | 0.2281 ns | 38.728 ns |  5.13 |    0.03 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 47.637 ns |  0.8412 ns | 0.6567 ns | 47.479 ns |  6.31 |    0.09 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  7.545 ns |  0.0319 ns | 0.0249 ns |  7.551 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |            |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 10.569 ns |  0.7891 ns | 0.0433 ns | 10.552 ns |  1.37 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 11.757 ns |  1.7773 ns | 0.0974 ns | 11.723 ns |  1.53 |    0.04 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  8.734 ns |  6.3273 ns | 0.3468 ns |  8.680 ns |  1.13 |    0.05 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  8.829 ns |  6.5619 ns | 0.3597 ns |  8.683 ns |  1.15 |    0.05 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  9.338 ns |  0.0522 ns | 0.0029 ns |  9.338 ns |  1.21 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 38.010 ns |  1.0837 ns | 0.0594 ns | 37.979 ns |  4.93 |    0.13 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 48.808 ns | 32.7024 ns | 1.7925 ns | 49.277 ns |  6.33 |    0.26 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  7.714 ns |  4.1733 ns | 0.2288 ns |  7.640 ns |  1.00 |    0.04 |         - |          NA |
