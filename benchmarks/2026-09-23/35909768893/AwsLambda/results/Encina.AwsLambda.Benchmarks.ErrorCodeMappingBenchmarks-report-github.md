```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.78GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.97 ns |  0.064 ns | 0.054 ns |  1.42 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.95 ns |  0.012 ns | 0.010 ns |  1.61 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.17 ns |  0.010 ns | 0.009 ns |  1.15 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.72 ns |  0.008 ns | 0.008 ns |  1.30 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.99 ns |  0.046 ns | 0.041 ns |  1.42 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.13 ns |  0.035 ns | 0.027 ns |  5.32 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.69 ns |  0.138 ns | 0.129 ns |  6.70 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.54 ns |  0.015 ns | 0.013 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |           |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.77 ns |  0.151 ns | 0.008 ns |  1.43 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.37 ns |  0.087 ns | 0.005 ns |  1.58 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.41 ns |  0.087 ns | 0.005 ns |  1.20 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.44 ns |  0.953 ns | 0.052 ns |  1.30 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.87 ns |  6.297 ns | 0.345 ns |  1.44 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.97 ns | 17.527 ns | 0.961 ns |  5.41 |    0.08 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 70.78 ns |  0.545 ns | 0.030 ns |  6.84 |    0.01 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.35 ns |  0.372 ns | 0.020 ns |  1.00 |    0.00 |         - |          NA |
