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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.570 ns |  0.1342 ns | 0.1256 ns |  1.40 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 11.006 ns |  0.1072 ns | 0.0895 ns |  1.61 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  7.593 ns |  0.0465 ns | 0.0363 ns |  1.11 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.917 ns |  0.1622 ns | 0.1593 ns |  1.16 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 35.718 ns |  0.3414 ns | 0.2851 ns |  5.23 |    0.05 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 41.367 ns |  0.4362 ns | 0.4080 ns |  6.06 |    0.06 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.828 ns |  0.0415 ns | 0.0346 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.717 ns |  0.1563 ns | 0.1462 ns |  0.98 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.563 ns |  0.6806 ns | 0.0373 ns |  1.43 |    0.14 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.733 ns |  1.1578 ns | 0.0635 ns |  1.61 |    0.16 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  7.045 ns |  1.8067 ns | 0.0990 ns |  1.05 |    0.11 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.195 ns |  1.5973 ns | 0.0876 ns |  1.23 |    0.12 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 35.421 ns |  2.0035 ns | 0.1098 ns |  5.30 |    0.53 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 40.854 ns |  8.5594 ns | 0.4692 ns |  6.12 |    0.61 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.743 ns | 15.0835 ns | 0.8268 ns |  1.01 |    0.15 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.749 ns |  2.1294 ns | 0.1167 ns |  1.01 |    0.10 |         - |          NA |
