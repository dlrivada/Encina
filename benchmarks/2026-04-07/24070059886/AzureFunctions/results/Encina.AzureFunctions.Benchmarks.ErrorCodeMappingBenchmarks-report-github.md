```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|---------:|---------:|------:|----------:|------------:|
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 16           | Default     |        11.17 ns | 0.023 ns | 0.019 ns |  1.00 |         - |          NA |
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16           | Default     |        15.84 ns | 0.012 ns | 0.010 ns |  1.42 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16           | Default     |        18.32 ns | 0.010 ns | 0.008 ns |  1.64 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 16           | Default     |        12.18 ns | 0.017 ns | 0.016 ns |  1.09 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 16           | Default     |        13.74 ns | 0.014 ns | 0.012 ns |  1.23 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 16           | Default     |        14.97 ns | 0.011 ns | 0.010 ns |  1.34 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 16           | Default     |        61.57 ns | 0.040 ns | 0.032 ns |  5.51 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |        70.72 ns | 0.053 ns | 0.050 ns |  6.33 |         - |          NA |
|                                 |            |                |             |             |              |             |                 |          |          |       |           |             |
| MapValidationError              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   950,160.00 ns |       NA | 0.000 ns |  1.00 |         - |          NA |
| MapAuthorizationUnauthenticated | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   940,352.00 ns |       NA | 0.000 ns |  0.99 |         - |          NA |
| MapAuthorizationForbidden       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   925,374.00 ns |       NA | 0.000 ns |  0.97 |         - |          NA |
| MapNotFoundError                | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   926,375.00 ns |       NA | 0.000 ns |  0.97 |         - |          NA |
| MapConflictError                | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   881,341.00 ns |       NA | 0.000 ns |  0.93 |         - |          NA |
| MapUnknownError                 | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   903,833.00 ns |       NA | 0.000 ns |  0.95 |         - |          NA |
| MapMultipleValidationErrors     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,002,878.00 ns |       NA | 0.000 ns |  1.06 |         - |          NA |
| MapMixedErrors                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   969,746.00 ns |       NA | 0.000 ns |  1.02 |         - |          NA |
