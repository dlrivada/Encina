```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.06 ns | 0.161 ns | 0.150 ns |  1.42 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.93 ns | 0.016 ns | 0.013 ns |  1.60 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.83 ns | 0.162 ns | 0.151 ns |  1.30 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.03 ns | 0.124 ns | 0.110 ns |  1.42 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.48 ns | 0.345 ns | 0.305 ns |  5.33 |    0.05 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 71.01 ns | 0.671 ns | 0.627 ns |  6.70 |    0.07 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.60 ns | 0.082 ns | 0.077 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.30 ns | 0.140 ns | 0.131 ns |  1.16 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.81 ns | 0.306 ns | 0.017 ns |  1.52 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.60 ns | 4.005 ns | 0.220 ns |  1.60 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.42 ns | 0.124 ns | 0.007 ns |  1.29 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.68 ns | 0.523 ns | 0.029 ns |  1.41 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.77 ns | 3.747 ns | 0.205 ns |  5.36 |    0.06 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 70.92 ns | 1.367 ns | 0.075 ns |  6.82 |    0.07 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.40 ns | 2.293 ns | 0.126 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.42 ns | 1.085 ns | 0.059 ns |  1.19 |    0.01 |         - |          NA |
