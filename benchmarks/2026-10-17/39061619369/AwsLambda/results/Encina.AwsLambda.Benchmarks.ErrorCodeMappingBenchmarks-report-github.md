```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.17 ns | 0.049 ns | 0.041 ns |  1.37 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.18 ns | 0.015 ns | 0.012 ns |  1.54 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.10 ns | 0.007 ns | 0.005 ns |  1.11 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.36 ns | 0.006 ns | 0.005 ns |  1.22 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.91 ns | 0.015 ns | 0.013 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 62.04 ns | 0.050 ns | 0.045 ns |  5.27 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 75.76 ns | 0.038 ns | 0.034 ns |  6.43 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.78 ns | 0.009 ns | 0.007 ns |  1.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.92 ns | 1.042 ns | 0.057 ns |  1.38 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.91 ns | 0.210 ns | 0.011 ns |  1.55 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.34 ns | 0.154 ns | 0.008 ns |  1.15 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.03 ns | 0.139 ns | 0.008 ns |  1.21 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.61 ns | 0.240 ns | 0.013 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.49 ns | 0.408 ns | 0.022 ns |  5.23 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 76.14 ns | 1.792 ns | 0.098 ns |  6.59 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.55 ns | 0.270 ns | 0.015 ns |  1.00 |         - |          NA |
