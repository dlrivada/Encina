```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.94 ns | 0.014 ns | 0.012 ns |  1.42 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.95 ns | 0.013 ns | 0.011 ns |  1.61 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.73 ns | 0.017 ns | 0.015 ns |  1.30 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.97 ns | 0.010 ns | 0.009 ns |  1.42 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.14 ns | 0.022 ns | 0.017 ns |  5.32 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.68 ns | 0.069 ns | 0.061 ns |  6.70 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.55 ns | 0.009 ns | 0.007 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.19 ns | 0.020 ns | 0.017 ns |  1.15 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.85 ns | 1.628 ns | 0.089 ns |  1.44 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.40 ns | 0.885 ns | 0.049 ns |  1.59 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.42 ns | 0.182 ns | 0.010 ns |  1.30 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.72 ns | 0.832 ns | 0.046 ns |  1.42 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.42 ns | 1.595 ns | 0.087 ns |  5.37 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 71.09 ns | 3.095 ns | 0.170 ns |  6.88 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.33 ns | 0.029 ns | 0.002 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.54 ns | 2.445 ns | 0.134 ns |  1.21 |         - |          NA |
