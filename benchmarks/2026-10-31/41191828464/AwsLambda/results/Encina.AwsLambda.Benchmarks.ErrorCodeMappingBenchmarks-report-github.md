```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.22 ns | 0.116 ns | 0.103 ns |  1.37 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.20 ns | 0.028 ns | 0.025 ns |  1.54 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.11 ns | 0.007 ns | 0.006 ns |  1.11 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.36 ns | 0.013 ns | 0.011 ns |  1.22 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.93 ns | 0.016 ns | 0.015 ns |  1.35 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.61 ns | 0.266 ns | 0.249 ns |  5.21 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 75.87 ns | 0.042 ns | 0.037 ns |  6.42 |    0.02 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.82 ns | 0.035 ns | 0.031 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 16.04 ns | 2.426 ns | 0.133 ns |  1.30 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.96 ns | 0.357 ns | 0.020 ns |  1.46 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.34 ns | 0.133 ns | 0.007 ns |  1.08 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.04 ns | 0.104 ns | 0.006 ns |  1.14 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.65 ns | 0.138 ns | 0.008 ns |  1.27 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.67 ns | 3.777 ns | 0.207 ns |  4.92 |    0.02 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 76.23 ns | 1.581 ns | 0.087 ns |  6.19 |    0.01 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 12.32 ns | 0.451 ns | 0.025 ns |  1.00 |    0.00 |         - |          NA |
