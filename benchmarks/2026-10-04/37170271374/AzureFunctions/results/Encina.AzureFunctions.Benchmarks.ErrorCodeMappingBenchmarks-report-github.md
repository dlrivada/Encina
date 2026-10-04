```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.218 ns | 0.1746 ns | 0.1715 ns |  1.44 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  9.175 ns | 0.1700 ns | 0.1507 ns |  1.61 |    0.04 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.448 ns | 0.0988 ns | 0.0825 ns |  1.13 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.463 ns | 0.1670 ns | 0.1562 ns |  1.31 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 30.142 ns | 0.4975 ns | 0.4653 ns |  5.28 |    0.12 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 36.218 ns | 0.2947 ns | 0.2613 ns |  6.34 |    0.11 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.712 ns | 0.1092 ns | 0.0968 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.209 ns | 0.1446 ns | 0.1775 ns |  1.09 |    0.04 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          |  8.788 ns | 0.2544 ns | 0.3648 ns |  1.55 |    0.07 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          |  9.212 ns | 0.1043 ns | 0.1495 ns |  1.62 |    0.04 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          |  6.537 ns | 0.0905 ns | 0.1269 ns |  1.15 |    0.03 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          |  7.885 ns | 0.1208 ns | 0.1693 ns |  1.39 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 30.457 ns | 0.3209 ns | 0.4283 ns |  5.37 |    0.11 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 37.214 ns | 0.3881 ns | 0.5566 ns |  6.56 |    0.14 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          |  5.670 ns | 0.0670 ns | 0.0940 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          |  5.934 ns | 0.1208 ns | 0.1808 ns |  1.05 |    0.04 |         - |          NA |
