```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.05GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.64 ns |  0.048 ns | 0.040 ns |  1.46 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 14.36 ns |  0.092 ns | 0.082 ns |  1.43 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 12.34 ns |  0.146 ns | 0.130 ns |  1.23 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 12.96 ns |  0.132 ns | 0.124 ns |  1.30 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 55.94 ns |  0.191 ns | 0.178 ns |  5.59 |    0.10 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 66.11 ns |  0.499 ns | 0.417 ns |  6.61 |    0.13 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.01 ns |  0.208 ns | 0.184 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 11.11 ns |  0.116 ns | 0.103 ns |  1.11 |    0.02 |         - |          NA |
|                                 |            |                |             |             |          |           |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.66 ns |  0.554 ns | 0.030 ns |  1.40 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 15.35 ns |  0.256 ns | 0.014 ns |  1.47 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.07 ns |  0.725 ns | 0.040 ns |  1.25 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 13.39 ns |  1.452 ns | 0.080 ns |  1.28 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 52.77 ns |  2.580 ns | 0.141 ns |  5.05 |    0.05 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 65.77 ns | 15.498 ns | 0.850 ns |  6.29 |    0.09 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.46 ns |  2.149 ns | 0.118 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 11.24 ns |  0.299 ns | 0.016 ns |  1.07 |    0.01 |         - |          NA |
