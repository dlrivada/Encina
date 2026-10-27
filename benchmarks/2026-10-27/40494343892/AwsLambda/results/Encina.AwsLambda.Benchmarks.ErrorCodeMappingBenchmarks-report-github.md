```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.17 ns |  0.019 ns | 0.015 ns |  1.37 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.17 ns |  0.007 ns | 0.006 ns |  1.54 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.11 ns |  0.025 ns | 0.023 ns |  1.11 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.39 ns |  0.055 ns | 0.051 ns |  1.22 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.90 ns |  0.013 ns | 0.012 ns |  1.35 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 62.07 ns |  0.919 ns | 0.815 ns |  5.26 |    0.07 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 75.91 ns |  0.179 ns | 0.149 ns |  6.44 |    0.03 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.79 ns |  0.058 ns | 0.048 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |           |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.91 ns |  0.134 ns | 0.007 ns |  1.37 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.95 ns |  0.225 ns | 0.012 ns |  1.55 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.36 ns |  0.257 ns | 0.014 ns |  1.15 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.06 ns |  0.656 ns | 0.036 ns |  1.21 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.60 ns |  0.148 ns | 0.008 ns |  1.35 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 61.34 ns | 18.548 ns | 1.017 ns |  5.30 |    0.08 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 77.17 ns | 27.851 ns | 1.527 ns |  6.66 |    0.12 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.58 ns |  0.908 ns | 0.050 ns |  1.00 |    0.01 |         - |          NA |
