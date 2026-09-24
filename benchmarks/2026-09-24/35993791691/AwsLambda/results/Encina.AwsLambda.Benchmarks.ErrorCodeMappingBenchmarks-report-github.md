```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.73 ns | 0.197 ns | 0.185 ns |  1.41 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.78 ns | 0.181 ns | 0.169 ns |  1.61 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 11.85 ns | 0.107 ns | 0.100 ns |  1.13 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.54 ns | 0.215 ns | 0.201 ns |  1.30 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.91 ns | 0.118 ns | 0.110 ns |  1.43 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 55.96 ns | 0.387 ns | 0.362 ns |  5.36 |    0.05 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 69.74 ns | 0.620 ns | 0.580 ns |  6.67 |    0.07 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.45 ns | 0.066 ns | 0.061 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.67 ns | 2.947 ns | 0.162 ns |  1.44 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.25 ns | 1.161 ns | 0.064 ns |  1.59 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.31 ns | 1.334 ns | 0.073 ns |  1.21 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.24 ns | 1.108 ns | 0.061 ns |  1.30 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.56 ns | 2.123 ns | 0.116 ns |  1.43 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.11 ns | 7.079 ns | 0.388 ns |  5.41 |    0.04 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 70.79 ns | 4.891 ns | 0.268 ns |  6.95 |    0.04 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.19 ns | 0.940 ns | 0.052 ns |  1.00 |    0.01 |         - |          NA |
