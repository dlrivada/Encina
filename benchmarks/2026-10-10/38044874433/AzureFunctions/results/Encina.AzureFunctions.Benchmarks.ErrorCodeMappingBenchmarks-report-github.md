```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.316 ns |  0.0357 ns | 0.0334 ns |  1.38 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.990 ns |  0.0535 ns | 0.0447 ns |  1.57 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  9.885 ns |  0.0440 ns | 0.0367 ns |  1.11 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 10.895 ns |  0.0681 ns | 0.0637 ns |  1.22 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 45.104 ns |  0.1838 ns | 0.1719 ns |  5.07 |    0.03 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 55.684 ns |  0.3664 ns | 0.3248 ns |  6.26 |    0.05 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.899 ns |  0.0495 ns | 0.0463 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.638 ns |  0.0286 ns | 0.0223 ns |  1.08 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.431 ns |  0.7453 ns | 0.0409 ns |  1.38 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 14.046 ns |  0.1155 ns | 0.0063 ns |  1.56 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.166 ns |  0.6060 ns | 0.0332 ns |  1.13 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.174 ns |  2.0022 ns | 0.1097 ns |  1.24 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 44.840 ns |  1.9723 ns | 0.1081 ns |  4.97 |    0.07 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 55.112 ns | 13.1444 ns | 0.7205 ns |  6.11 |    0.11 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  9.022 ns |  2.6884 ns | 0.1474 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.909 ns |  0.1915 ns | 0.0105 ns |  1.10 |    0.02 |         - |          NA |
