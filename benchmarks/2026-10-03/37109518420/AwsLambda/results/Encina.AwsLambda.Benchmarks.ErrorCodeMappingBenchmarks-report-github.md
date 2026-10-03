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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.97 ns | 0.035 ns | 0.031 ns |  1.42 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.96 ns | 0.023 ns | 0.019 ns |  1.61 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.17 ns | 0.013 ns | 0.010 ns |  1.15 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.73 ns | 0.011 ns | 0.009 ns |  1.30 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.98 ns | 0.017 ns | 0.013 ns |  1.42 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.20 ns | 0.073 ns | 0.057 ns |  5.32 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.70 ns | 0.065 ns | 0.057 ns |  6.69 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.56 ns | 0.011 ns | 0.008 ns |  1.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.83 ns | 0.943 ns | 0.052 ns |  1.43 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.37 ns | 0.153 ns | 0.008 ns |  1.58 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.41 ns | 0.095 ns | 0.005 ns |  1.20 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.44 ns | 0.316 ns | 0.017 ns |  1.30 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.68 ns | 0.300 ns | 0.016 ns |  1.42 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.39 ns | 0.382 ns | 0.021 ns |  5.35 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 70.97 ns | 2.276 ns | 0.125 ns |  6.86 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.34 ns | 0.101 ns | 0.006 ns |  1.00 |         - |          NA |
