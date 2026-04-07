```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 16           | Default     |        11.62 ns | 0.099 ns | 0.092 ns |  1.00 |    0.01 |         - |          NA |
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16           | Default     |        15.59 ns | 0.053 ns | 0.044 ns |  1.34 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16           | Default     |        16.98 ns | 0.033 ns | 0.030 ns |  1.46 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 16           | Default     |        13.35 ns | 0.181 ns | 0.169 ns |  1.15 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 16           | Default     |        13.65 ns | 0.165 ns | 0.147 ns |  1.17 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 16           | Default     |        14.54 ns | 0.244 ns | 0.204 ns |  1.25 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 16           | Default     |        60.74 ns | 0.171 ns | 0.142 ns |  5.23 |    0.04 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |        70.34 ns | 0.136 ns | 0.114 ns |  6.05 |    0.05 |         - |          NA |
|                                 |            |                |             |             |              |             |                 |          |          |       |         |           |             |
| MapValidationError              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   901,473.00 ns |       NA | 0.000 ns |  1.00 |    0.00 |         - |          NA |
| MapAuthorizationUnauthenticated | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   911,911.00 ns |       NA | 0.000 ns |  1.01 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   920,329.00 ns |       NA | 0.000 ns |  1.02 |    0.00 |         - |          NA |
| MapNotFoundError                | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   917,239.00 ns |       NA | 0.000 ns |  1.02 |    0.00 |         - |          NA |
| MapConflictError                | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   919,927.00 ns |       NA | 0.000 ns |  1.02 |    0.00 |         - |          NA |
| MapUnknownError                 | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   919,098.00 ns |       NA | 0.000 ns |  1.02 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,036,000.00 ns |       NA | 0.000 ns |  1.15 |    0.00 |         - |          NA |
| MapMixedErrors                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   939,595.00 ns |       NA | 0.000 ns |  1.04 |    0.00 |         - |          NA |
