```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.06GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.92 ns | 0.199 ns | 0.187 ns |  1.45 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 15.53 ns | 0.108 ns | 0.096 ns |  1.51 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 12.84 ns | 0.302 ns | 0.371 ns |  1.25 |    0.04 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 13.31 ns | 0.238 ns | 0.223 ns |  1.29 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 58.22 ns | 0.654 ns | 0.580 ns |  5.66 |    0.14 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 68.20 ns | 0.160 ns | 0.150 ns |  6.63 |    0.15 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.29 ns | 0.243 ns | 0.228 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 11.28 ns | 0.148 ns | 0.138 ns |  1.10 |    0.03 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.69 ns | 0.803 ns | 0.044 ns |  1.39 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.16 ns | 0.399 ns | 0.022 ns |  1.53 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 12.86 ns | 2.440 ns | 0.134 ns |  1.22 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.10 ns | 1.175 ns | 0.064 ns |  1.33 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 54.84 ns | 2.413 ns | 0.132 ns |  5.19 |    0.02 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 64.99 ns | 9.110 ns | 0.499 ns |  6.14 |    0.05 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.58 ns | 0.873 ns | 0.048 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 11.56 ns | 1.600 ns | 0.088 ns |  1.09 |    0.01 |         - |          NA |
