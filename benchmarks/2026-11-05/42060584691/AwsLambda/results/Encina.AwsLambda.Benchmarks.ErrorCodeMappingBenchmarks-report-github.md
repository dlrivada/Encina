```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.89GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.97 ns | 0.032 ns | 0.028 ns |  1.36 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 17.92 ns | 0.012 ns | 0.010 ns |  1.52 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.72 ns | 0.017 ns | 0.015 ns |  1.08 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.67 ns | 0.067 ns | 0.060 ns |  1.16 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.66 ns | 0.023 ns | 0.020 ns |  1.25 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 63.70 ns | 0.226 ns | 0.212 ns |  5.42 |    0.03 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 77.18 ns | 0.258 ns | 0.241 ns |  6.56 |    0.04 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.76 ns | 0.074 ns | 0.062 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 16.20 ns | 0.352 ns | 0.019 ns |  1.38 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.95 ns | 0.080 ns | 0.004 ns |  1.53 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.68 ns | 0.087 ns | 0.005 ns |  1.08 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.75 ns | 0.350 ns | 0.019 ns |  1.17 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.37 ns | 0.264 ns | 0.014 ns |  1.22 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 66.01 ns | 0.463 ns | 0.025 ns |  5.62 |    0.00 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 77.96 ns | 2.000 ns | 0.110 ns |  6.64 |    0.01 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.74 ns | 0.126 ns | 0.007 ns |  1.00 |    0.00 |         - |          NA |
