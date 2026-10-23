```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 13.62 ns |  0.019 ns | 0.017 ns |  1.26 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 14.31 ns |  0.307 ns | 0.341 ns |  1.33 |    0.04 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 11.08 ns |  0.247 ns | 0.445 ns |  1.03 |    0.05 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.62 ns |  0.261 ns | 0.311 ns |  1.08 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 49.96 ns |  0.754 ns | 0.705 ns |  4.62 |    0.12 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 63.22 ns |  1.263 ns | 1.929 ns |  5.85 |    0.22 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.81 ns |  0.236 ns | 0.242 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 10.74 ns |  0.214 ns | 0.263 ns |  0.99 |    0.03 |         - |          NA |
|                                 |            |                |             |             |          |           |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.10 ns |  0.622 ns | 0.034 ns |  1.34 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 15.54 ns | 17.199 ns | 0.943 ns |  1.48 |    0.08 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 11.33 ns |  1.175 ns | 0.064 ns |  1.08 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 12.43 ns |  5.931 ns | 0.325 ns |  1.18 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 49.93 ns |  9.663 ns | 0.530 ns |  4.74 |    0.06 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 63.94 ns | 27.372 ns | 1.500 ns |  6.07 |    0.14 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.53 ns |  2.178 ns | 0.119 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 11.39 ns |  8.470 ns | 0.464 ns |  1.08 |    0.04 |         - |          NA |
