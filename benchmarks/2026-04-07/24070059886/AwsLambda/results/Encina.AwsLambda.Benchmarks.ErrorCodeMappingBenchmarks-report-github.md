```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 3.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 16           | Default     |      11.16 ns | 0.024 ns | 0.020 ns |  1.00 |    0.00 |         - |          NA |
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16           | Default     |      15.84 ns | 0.029 ns | 0.026 ns |  1.42 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16           | Default     |      18.32 ns | 0.021 ns | 0.018 ns |  1.64 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 16           | Default     |      11.96 ns | 0.027 ns | 0.024 ns |  1.07 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 16           | Default     |      13.73 ns | 0.011 ns | 0.008 ns |  1.23 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 16           | Default     |      15.01 ns | 0.053 ns | 0.049 ns |  1.34 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 16           | Default     |      61.89 ns | 0.580 ns | 0.542 ns |  5.55 |    0.05 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |      71.96 ns | 1.053 ns | 0.985 ns |  6.45 |    0.09 |         - |          NA |
|                                 |            |                |             |             |              |             |               |          |          |       |         |           |             |
| MapValidationError              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 889,656.00 ns |       NA | 0.000 ns |  1.00 |    0.00 |         - |          NA |
| MapAuthorizationUnauthenticated | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 886,871.00 ns |       NA | 0.000 ns |  1.00 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 906,367.00 ns |       NA | 0.000 ns |  1.02 |    0.00 |         - |          NA |
| MapNotFoundError                | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 895,577.00 ns |       NA | 0.000 ns |  1.01 |    0.00 |         - |          NA |
| MapConflictError                | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 924,612.00 ns |       NA | 0.000 ns |  1.04 |    0.00 |         - |          NA |
| MapUnknownError                 | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 889,065.00 ns |       NA | 0.000 ns |  1.00 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 993,060.00 ns |       NA | 0.000 ns |  1.12 |    0.00 |         - |          NA |
| MapMixedErrors                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 948,286.00 ns |       NA | 0.000 ns |  1.07 |    0.00 |         - |          NA |
