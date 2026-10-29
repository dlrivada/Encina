```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.56GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.11 ns |  0.254 ns | 0.238 ns |  1.38 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 15.82 ns |  0.234 ns | 0.207 ns |  1.45 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 11.62 ns |  0.157 ns | 0.146 ns |  1.06 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.75 ns |  0.264 ns | 0.527 ns |  1.08 |    0.05 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 47.94 ns |  0.802 ns | 0.711 ns |  4.39 |    0.08 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 62.82 ns |  1.239 ns | 1.326 ns |  5.75 |    0.13 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.93 ns |  0.129 ns | 0.121 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 11.06 ns |  0.165 ns | 0.138 ns |  1.01 |    0.02 |         - |          NA |
|                                 |            |                |             |             |          |           |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.20 ns |  5.840 ns | 0.320 ns |  1.38 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 15.77 ns |  4.742 ns | 0.260 ns |  1.43 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 11.71 ns |  5.511 ns | 0.302 ns |  1.06 |    0.03 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.72 ns |  3.078 ns | 0.169 ns |  1.06 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 52.99 ns | 11.938 ns | 0.654 ns |  4.80 |    0.06 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 66.22 ns | 30.399 ns | 1.666 ns |  6.00 |    0.14 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.04 ns |  1.826 ns | 0.100 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 11.52 ns |  4.375 ns | 0.240 ns |  1.04 |    0.02 |         - |          NA |
