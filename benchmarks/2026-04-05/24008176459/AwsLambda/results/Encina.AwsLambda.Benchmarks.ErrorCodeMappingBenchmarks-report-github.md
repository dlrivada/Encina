```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.82GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|---------:|---------:|------:|----------:|------------:|
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 16           | Default     |      11.16 ns | 0.020 ns | 0.017 ns |  1.00 |         - |          NA |
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16           | Default     |      15.85 ns | 0.035 ns | 0.027 ns |  1.42 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16           | Default     |      18.35 ns | 0.045 ns | 0.040 ns |  1.64 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 16           | Default     |      12.18 ns | 0.025 ns | 0.020 ns |  1.09 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 16           | Default     |      13.73 ns | 0.012 ns | 0.011 ns |  1.23 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 16           | Default     |      15.02 ns | 0.050 ns | 0.045 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 16           | Default     |      61.57 ns | 0.045 ns | 0.040 ns |  5.52 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |      70.70 ns | 0.080 ns | 0.074 ns |  6.34 |         - |          NA |
|                                 |            |                |             |             |              |             |               |          |          |       |           |             |
| MapValidationError              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 902,774.00 ns |       NA | 0.000 ns |  1.00 |         - |          NA |
| MapAuthorizationUnauthenticated | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 891,292.00 ns |       NA | 0.000 ns |  0.99 |         - |          NA |
| MapAuthorizationForbidden       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 944,261.00 ns |       NA | 0.000 ns |  1.05 |         - |          NA |
| MapNotFoundError                | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 910,357.00 ns |       NA | 0.000 ns |  1.01 |         - |          NA |
| MapConflictError                | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 937,218.00 ns |       NA | 0.000 ns |  1.04 |         - |          NA |
| MapUnknownError                 | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 908,925.00 ns |       NA | 0.000 ns |  1.01 |         - |          NA |
| MapMultipleValidationErrors     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 988,764.00 ns |       NA | 0.000 ns |  1.10 |         - |          NA |
| MapMixedErrors                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 949,992.00 ns |       NA | 0.000 ns |  1.05 |         - |          NA |
