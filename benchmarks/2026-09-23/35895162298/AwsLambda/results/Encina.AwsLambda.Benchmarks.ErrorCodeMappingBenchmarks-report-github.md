```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.30 ns | 0.048 ns | 0.045 ns |  1.34 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.20 ns | 0.017 ns | 0.015 ns |  1.52 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.18 ns | 0.087 ns | 0.081 ns |  1.23 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 12.98 ns | 0.151 ns | 0.134 ns |  1.22 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.17 ns | 0.150 ns | 0.141 ns |  1.42 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.37 ns | 0.168 ns | 0.140 ns |  5.28 |    0.05 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 72.71 ns | 0.500 ns | 0.467 ns |  6.81 |    0.08 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.68 ns | 0.117 ns | 0.104 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.53 ns | 0.268 ns | 0.015 ns |  1.38 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.49 ns | 0.455 ns | 0.025 ns |  1.56 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 11.68 ns | 0.430 ns | 0.024 ns |  1.11 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.13 ns | 0.240 ns | 0.013 ns |  1.25 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.60 ns | 0.107 ns | 0.006 ns |  1.38 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 57.25 ns | 5.962 ns | 0.327 ns |  5.43 |    0.05 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 68.10 ns | 3.685 ns | 0.202 ns |  6.46 |    0.06 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.54 ns | 1.866 ns | 0.102 ns |  1.00 |    0.01 |         - |          NA |
