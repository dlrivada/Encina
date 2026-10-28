```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.08 ns |  0.054 ns | 0.048 ns |  1.45 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 17.78 ns |  0.011 ns | 0.009 ns |  1.60 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.36 ns |  0.011 ns | 0.009 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.54 ns |  0.017 ns | 0.015 ns |  1.40 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 60.38 ns |  0.037 ns | 0.031 ns |  5.44 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 73.61 ns |  0.097 ns | 0.086 ns |  6.63 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.11 ns |  0.015 ns | 0.014 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.71 ns |  0.017 ns | 0.015 ns |  1.14 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |           |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 16.06 ns |  0.685 ns | 0.038 ns |  1.41 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.86 ns |  0.176 ns | 0.010 ns |  1.57 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.35 ns |  0.164 ns | 0.009 ns |  1.17 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.71 ns |  0.409 ns | 0.022 ns |  1.38 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.37 ns |  0.393 ns | 0.022 ns |  5.31 |    0.00 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 75.57 ns | 10.932 ns | 0.599 ns |  6.65 |    0.05 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.37 ns |  0.180 ns | 0.010 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.69 ns |  0.083 ns | 0.005 ns |  1.12 |    0.00 |         - |          NA |
