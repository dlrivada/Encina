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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.97 ns | 0.027 ns | 0.023 ns |  1.42 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.95 ns | 0.027 ns | 0.023 ns |  1.61 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.75 ns | 0.045 ns | 0.040 ns |  1.30 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.97 ns | 0.011 ns | 0.010 ns |  1.42 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.15 ns | 0.021 ns | 0.017 ns |  5.32 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.64 ns | 0.038 ns | 0.030 ns |  6.69 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.55 ns | 0.008 ns | 0.006 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.18 ns | 0.019 ns | 0.017 ns |  1.15 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.78 ns | 0.109 ns | 0.006 ns |  1.41 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.44 ns | 0.225 ns | 0.012 ns |  1.57 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.43 ns | 0.557 ns | 0.031 ns |  1.28 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.71 ns | 0.849 ns | 0.047 ns |  1.41 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 56.15 ns | 0.305 ns | 0.017 ns |  5.37 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 71.01 ns | 2.670 ns | 0.146 ns |  6.79 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.46 ns | 0.102 ns | 0.006 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.41 ns | 0.378 ns | 0.021 ns |  1.19 |         - |          NA |
