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
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 16           | Default     |      11.14 ns | 0.020 ns | 0.015 ns |  1.00 |    0.00 |         - |          NA |
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16           | Default     |      15.82 ns | 0.024 ns | 0.020 ns |  1.42 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16           | Default     |      18.35 ns | 0.039 ns | 0.037 ns |  1.65 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 16           | Default     |      12.16 ns | 0.007 ns | 0.007 ns |  1.09 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 16           | Default     |      13.72 ns | 0.013 ns | 0.012 ns |  1.23 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 16           | Default     |      14.97 ns | 0.006 ns | 0.005 ns |  1.34 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 16           | Default     |      61.51 ns | 0.182 ns | 0.152 ns |  5.52 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |      70.69 ns | 0.135 ns | 0.120 ns |  6.35 |    0.01 |         - |          NA |
|                                 |            |                |             |             |              |             |               |          |          |       |         |           |             |
| MapValidationError              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 885,520.00 ns |       NA | 0.000 ns |  1.00 |    0.00 |         - |          NA |
| MapAuthorizationUnauthenticated | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 874,429.00 ns |       NA | 0.000 ns |  0.99 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 890,199.00 ns |       NA | 0.000 ns |  1.01 |    0.00 |         - |          NA |
| MapNotFoundError                | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 874,850.00 ns |       NA | 0.000 ns |  0.99 |    0.00 |         - |          NA |
| MapConflictError                | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 871,654.00 ns |       NA | 0.000 ns |  0.98 |    0.00 |         - |          NA |
| MapUnknownError                 | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 854,482.00 ns |       NA | 0.000 ns |  0.96 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 963,105.00 ns |       NA | 0.000 ns |  1.09 |    0.00 |         - |          NA |
| MapMixedErrors                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 929,062.00 ns |       NA | 0.000 ns |  1.05 |    0.00 |         - |          NA |
