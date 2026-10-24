```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.529 ns |  0.1579 ns | 0.1477 ns |  1.45 |    0.06 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 11.032 ns |  0.1218 ns | 0.1139 ns |  1.68 |    0.06 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.736 ns |  0.1376 ns | 0.1149 ns |  1.02 |    0.04 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.996 ns |  0.1441 ns | 0.1415 ns |  1.06 |    0.04 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.923 ns |  0.1759 ns | 0.1645 ns |  1.20 |    0.05 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 36.056 ns |  0.4458 ns | 0.4170 ns |  5.48 |    0.20 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 40.944 ns |  0.4123 ns | 0.3856 ns |  6.23 |    0.23 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.583 ns |  0.1537 ns | 0.2393 ns |  1.00 |    0.05 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.412 ns |  2.6588 ns | 0.1457 ns |  1.48 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.930 ns |  0.8713 ns | 0.0478 ns |  1.71 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.936 ns |  5.4104 ns | 0.2966 ns |  1.09 |    0.04 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  7.007 ns |  1.7819 ns | 0.0977 ns |  1.10 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.241 ns |  1.4426 ns | 0.0791 ns |  1.29 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 35.994 ns |  4.3902 ns | 0.2406 ns |  5.65 |    0.05 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 41.533 ns | 11.0135 ns | 0.6037 ns |  6.51 |    0.09 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.376 ns |  0.9465 ns | 0.0519 ns |  1.00 |    0.01 |         - |          NA |
