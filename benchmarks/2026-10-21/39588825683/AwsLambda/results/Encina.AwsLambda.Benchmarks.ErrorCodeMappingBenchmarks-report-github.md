```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 13.98 ns | 0.150 ns | 0.133 ns |  1.37 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 14.79 ns | 0.318 ns | 0.504 ns |  1.44 |    0.05 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 11.13 ns | 0.249 ns | 0.531 ns |  1.09 |    0.05 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 11.17 ns | 0.064 ns | 0.054 ns |  1.09 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.69 ns | 0.243 ns | 0.228 ns |  1.14 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 47.95 ns | 0.926 ns | 0.867 ns |  4.68 |    0.10 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 61.83 ns | 0.965 ns | 0.806 ns |  6.04 |    0.10 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.24 ns | 0.144 ns | 0.112 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.50 ns | 4.157 ns | 0.228 ns |  1.43 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 14.75 ns | 4.700 ns | 0.258 ns |  1.46 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 10.73 ns | 2.749 ns | 0.151 ns |  1.06 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 11.12 ns | 6.096 ns | 0.334 ns |  1.10 |    0.03 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.92 ns | 7.065 ns | 0.387 ns |  1.18 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 48.44 ns | 0.892 ns | 0.049 ns |  4.78 |    0.01 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 59.50 ns | 5.780 ns | 0.317 ns |  5.87 |    0.03 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.13 ns | 0.233 ns | 0.013 ns |  1.00 |    0.00 |         - |          NA |
