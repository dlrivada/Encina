```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.97 ns | 0.010 ns | 0.008 ns |  1.41 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.95 ns | 0.024 ns | 0.020 ns |  1.60 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.16 ns | 0.013 ns | 0.012 ns |  1.24 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.74 ns | 0.026 ns | 0.021 ns |  1.30 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.00 ns | 0.055 ns | 0.049 ns |  1.42 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.15 ns | 0.016 ns | 0.013 ns |  5.30 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.70 ns | 0.046 ns | 0.040 ns |  6.67 |    0.02 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.59 ns | 0.045 ns | 0.040 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.82 ns | 1.476 ns | 0.081 ns |  1.43 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.38 ns | 0.283 ns | 0.016 ns |  1.58 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.41 ns | 0.138 ns | 0.008 ns |  1.20 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.42 ns | 0.199 ns | 0.011 ns |  1.30 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.68 ns | 0.693 ns | 0.038 ns |  1.42 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.38 ns | 0.536 ns | 0.029 ns |  5.35 |    0.01 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 70.89 ns | 0.398 ns | 0.022 ns |  6.84 |    0.02 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.36 ns | 0.564 ns | 0.031 ns |  1.00 |    0.00 |         - |          NA |
