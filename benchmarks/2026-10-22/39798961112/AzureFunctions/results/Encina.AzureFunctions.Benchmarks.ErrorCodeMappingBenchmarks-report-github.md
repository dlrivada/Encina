```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.923 ns | 0.1995 ns | 0.2135 ns |  1.51 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.109 ns | 0.1664 ns | 0.1475 ns |  1.71 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.511 ns | 0.1025 ns | 0.0958 ns |  1.10 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.282 ns | 0.0632 ns | 0.0561 ns |  1.24 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 33.104 ns | 0.1798 ns | 0.1594 ns |  5.61 |    0.06 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 37.969 ns | 0.3428 ns | 0.2862 ns |  6.44 |    0.08 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.897 ns | 0.0682 ns | 0.0605 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.230 ns | 0.1174 ns | 0.0981 ns |  1.06 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          |  8.865 ns | 0.0877 ns | 0.1258 ns |  1.49 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 10.160 ns | 0.0899 ns | 0.1260 ns |  1.71 |    0.05 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          |  6.607 ns | 0.1188 ns | 0.1742 ns |  1.11 |    0.04 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          |  7.666 ns | 0.1544 ns | 0.2113 ns |  1.29 |    0.05 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 33.102 ns | 0.2872 ns | 0.4209 ns |  5.56 |    0.15 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 37.997 ns | 0.1890 ns | 0.2711 ns |  6.38 |    0.16 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          |  5.957 ns | 0.1037 ns | 0.1487 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          |  6.266 ns | 0.0563 ns | 0.0843 ns |  1.05 |    0.03 |         - |          NA |
