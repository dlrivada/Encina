```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.241 ns |  0.1195 ns | 0.0998 ns |  1.48 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.658 ns |  0.2331 ns | 0.2289 ns |  1.70 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.927 ns |  0.1525 ns | 0.1427 ns |  1.11 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.830 ns |  0.1228 ns | 0.1089 ns |  1.09 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.720 ns |  0.1284 ns | 0.1072 ns |  1.23 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 34.512 ns |  0.4110 ns | 0.3209 ns |  5.51 |    0.09 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 39.809 ns |  0.7042 ns | 0.6587 ns |  6.36 |    0.14 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.260 ns |  0.0997 ns | 0.0933 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.172 ns |  1.0544 ns | 0.0578 ns |  1.48 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 11.190 ns | 11.0316 ns | 0.6047 ns |  1.81 |    0.09 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.698 ns |  0.2327 ns | 0.0128 ns |  1.08 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.999 ns |  1.6423 ns | 0.0900 ns |  1.13 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.865 ns |  3.0020 ns | 0.1646 ns |  1.27 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 34.979 ns |  3.2847 ns | 0.1800 ns |  5.66 |    0.05 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 40.180 ns | 13.6083 ns | 0.7459 ns |  6.50 |    0.12 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.185 ns |  0.9883 ns | 0.0542 ns |  1.00 |    0.01 |         - |          NA |
