```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.16 ns |  0.014 ns | 0.011 ns |  1.37 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.20 ns |  0.018 ns | 0.015 ns |  1.54 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.12 ns |  0.016 ns | 0.013 ns |  1.11 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.38 ns |  0.031 ns | 0.028 ns |  1.22 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.93 ns |  0.051 ns | 0.040 ns |  1.35 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.47 ns |  0.100 ns | 0.083 ns |  5.21 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 81.54 ns |  0.173 ns | 0.153 ns |  6.92 |    0.02 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.79 ns |  0.018 ns | 0.015 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |           |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.99 ns |  1.150 ns | 0.063 ns |  1.38 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.95 ns |  0.720 ns | 0.039 ns |  1.55 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.36 ns |  0.228 ns | 0.012 ns |  1.16 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.07 ns |  0.168 ns | 0.009 ns |  1.22 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.65 ns |  0.942 ns | 0.052 ns |  1.35 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 61.95 ns |  5.621 ns | 0.308 ns |  5.36 |    0.02 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 77.55 ns | 10.504 ns | 0.576 ns |  6.71 |    0.04 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.56 ns |  0.215 ns | 0.012 ns |  1.00 |    0.00 |         - |          NA |
