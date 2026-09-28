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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.12 ns | 0.032 ns | 0.030 ns |  1.42 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 17.12 ns | 0.030 ns | 0.027 ns |  1.60 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.87 ns | 0.017 ns | 0.014 ns |  1.30 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.98 ns | 0.011 ns | 0.009 ns |  1.40 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.44 ns | 0.354 ns | 0.331 ns |  5.28 |    0.03 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 71.42 ns | 0.082 ns | 0.077 ns |  6.69 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.68 ns | 0.020 ns | 0.019 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.34 ns | 0.066 ns | 0.055 ns |  1.16 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.95 ns | 0.111 ns | 0.006 ns |  1.45 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.70 ns | 3.257 ns | 0.179 ns |  1.62 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.44 ns | 0.503 ns | 0.028 ns |  1.31 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.84 ns | 1.031 ns | 0.057 ns |  1.44 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.44 ns | 1.104 ns | 0.061 ns |  5.39 |    0.01 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 71.64 ns | 0.472 ns | 0.026 ns |  6.97 |    0.01 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.28 ns | 0.418 ns | 0.023 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.53 ns | 0.218 ns | 0.012 ns |  1.22 |    0.00 |         - |          NA |
