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
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 16           | Default     |      11.22 ns | 0.101 ns | 0.084 ns |  1.00 |    0.01 |         - |          NA |
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16           | Default     |      15.84 ns | 0.024 ns | 0.020 ns |  1.41 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16           | Default     |      18.42 ns | 0.014 ns | 0.012 ns |  1.64 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 16           | Default     |      12.19 ns | 0.027 ns | 0.025 ns |  1.09 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 16           | Default     |      13.73 ns | 0.012 ns | 0.010 ns |  1.22 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 16           | Default     |      14.99 ns | 0.015 ns | 0.014 ns |  1.34 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 16           | Default     |      61.67 ns | 0.132 ns | 0.111 ns |  5.50 |    0.04 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |      70.80 ns | 0.209 ns | 0.174 ns |  6.31 |    0.05 |         - |          NA |
|                                 |            |                |             |             |              |             |               |          |          |       |         |           |             |
| MapValidationError              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 888,017.00 ns |       NA | 0.000 ns |  1.00 |    0.00 |         - |          NA |
| MapAuthorizationUnauthenticated | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 886,634.00 ns |       NA | 0.000 ns |  1.00 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 908,345.00 ns |       NA | 0.000 ns |  1.02 |    0.00 |         - |          NA |
| MapNotFoundError                | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 930,416.00 ns |       NA | 0.000 ns |  1.05 |    0.00 |         - |          NA |
| MapConflictError                | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 875,373.00 ns |       NA | 0.000 ns |  0.99 |    0.00 |         - |          NA |
| MapUnknownError                 | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 881,344.00 ns |       NA | 0.000 ns |  0.99 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 958,389.00 ns |       NA | 0.000 ns |  1.08 |    0.00 |         - |          NA |
| MapMixedErrors                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 939,143.00 ns |       NA | 0.000 ns |  1.06 |    0.00 |         - |          NA |
