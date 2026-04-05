```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 16           | Default     |      11.17 ns | 0.026 ns | 0.023 ns |  1.00 |    0.00 |         - |          NA |
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16           | Default     |      15.85 ns | 0.044 ns | 0.034 ns |  1.42 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16           | Default     |      18.33 ns | 0.012 ns | 0.010 ns |  1.64 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 16           | Default     |      12.17 ns | 0.006 ns | 0.005 ns |  1.09 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 16           | Default     |      13.75 ns | 0.060 ns | 0.047 ns |  1.23 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 16           | Default     |      15.00 ns | 0.011 ns | 0.009 ns |  1.34 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 16           | Default     |      61.61 ns | 0.052 ns | 0.043 ns |  5.52 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |      70.73 ns | 0.230 ns | 0.179 ns |  6.33 |    0.02 |         - |          NA |
|                                 |            |                |             |             |              |             |               |          |          |       |         |           |             |
| MapValidationError              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 904,375.00 ns |       NA | 0.000 ns |  1.00 |    0.00 |         - |          NA |
| MapAuthorizationUnauthenticated | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 898,293.00 ns |       NA | 0.000 ns |  0.99 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 876,403.00 ns |       NA | 0.000 ns |  0.97 |    0.00 |         - |          NA |
| MapNotFoundError                | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 912,259.00 ns |       NA | 0.000 ns |  1.01 |    0.00 |         - |          NA |
| MapConflictError                | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 885,595.00 ns |       NA | 0.000 ns |  0.98 |    0.00 |         - |          NA |
| MapUnknownError                 | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 912,826.00 ns |       NA | 0.000 ns |  1.01 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 983,007.00 ns |       NA | 0.000 ns |  1.09 |    0.00 |         - |          NA |
| MapMixedErrors                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 939,145.00 ns |       NA | 0.000 ns |  1.04 |    0.00 |         - |          NA |
