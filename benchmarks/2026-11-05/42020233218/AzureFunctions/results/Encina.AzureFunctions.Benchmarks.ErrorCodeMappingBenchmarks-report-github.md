```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.78GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.08 ns | 0.014 ns | 0.012 ns |  1.33 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 17.10 ns | 0.206 ns | 0.193 ns |  1.51 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.46 ns | 0.018 ns | 0.017 ns |  1.19 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 13.88 ns | 0.015 ns | 0.013 ns |  1.22 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 59.24 ns | 0.066 ns | 0.058 ns |  5.22 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 69.24 ns | 0.154 ns | 0.120 ns |  6.11 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.34 ns | 0.016 ns | 0.015 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.26 ns | 0.017 ns | 0.014 ns |  1.08 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.07 ns | 0.262 ns | 0.014 ns |  1.34 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.81 ns | 0.485 ns | 0.027 ns |  1.50 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.55 ns | 0.856 ns | 0.047 ns |  1.21 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.61 ns | 0.302 ns | 0.017 ns |  1.30 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 59.08 ns | 1.685 ns | 0.092 ns |  5.26 |    0.02 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 69.18 ns | 2.273 ns | 0.125 ns |  6.16 |    0.02 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.23 ns | 0.751 ns | 0.041 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.34 ns | 1.084 ns | 0.059 ns |  1.10 |    0.01 |         - |          NA |
