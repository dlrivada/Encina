```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.179 ns |  0.1961 ns | 0.2014 ns |  1.48 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.642 ns |  0.2284 ns | 0.2630 ns |  1.71 |    0.05 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.593 ns |  0.1011 ns | 0.0896 ns |  1.06 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.587 ns |  0.0922 ns | 0.0720 ns |  1.06 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.440 ns |  0.1440 ns | 0.1203 ns |  1.20 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 34.541 ns |  0.6248 ns | 0.6136 ns |  5.57 |    0.11 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 38.953 ns |  0.3923 ns | 0.3276 ns |  6.28 |    0.09 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.207 ns |  0.0792 ns | 0.0702 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.999 ns |  1.3632 ns | 0.0747 ns |  1.36 |    0.09 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.345 ns |  0.5299 ns | 0.0290 ns |  1.57 |    0.10 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.479 ns |  1.4534 ns | 0.0797 ns |  0.98 |    0.06 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.802 ns |  2.1534 ns | 0.1180 ns |  1.03 |    0.07 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.790 ns |  3.4569 ns | 0.1895 ns |  1.18 |    0.08 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 33.291 ns |  7.6237 ns | 0.4179 ns |  5.05 |    0.32 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 40.040 ns | 26.3495 ns | 1.4443 ns |  6.07 |    0.43 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.620 ns |  9.1317 ns | 0.5005 ns |  1.00 |    0.09 |         - |          NA |
