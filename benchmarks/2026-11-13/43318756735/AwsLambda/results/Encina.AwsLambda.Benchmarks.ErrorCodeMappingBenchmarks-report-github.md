```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.95 ns |  0.028 ns | 0.022 ns |  1.42 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 17.01 ns |  0.139 ns | 0.130 ns |  1.61 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.26 ns |  0.078 ns | 0.073 ns |  1.16 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.72 ns |  0.027 ns | 0.021 ns |  1.30 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.07 ns |  0.137 ns | 0.128 ns |  1.43 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.23 ns |  0.185 ns | 0.164 ns |  5.33 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.63 ns |  0.089 ns | 0.083 ns |  6.70 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.55 ns |  0.009 ns | 0.007 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |           |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.79 ns |  0.147 ns | 0.008 ns |  1.42 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.62 ns |  6.036 ns | 0.331 ns |  1.60 |    0.03 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.47 ns |  1.649 ns | 0.090 ns |  1.20 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.41 ns |  0.112 ns | 0.006 ns |  1.29 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.81 ns |  2.667 ns | 0.146 ns |  1.43 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.81 ns | 10.768 ns | 0.590 ns |  5.37 |    0.05 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 71.14 ns |  4.872 ns | 0.267 ns |  6.85 |    0.03 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.39 ns |  0.654 ns | 0.036 ns |  1.00 |    0.00 |         - |          NA |
