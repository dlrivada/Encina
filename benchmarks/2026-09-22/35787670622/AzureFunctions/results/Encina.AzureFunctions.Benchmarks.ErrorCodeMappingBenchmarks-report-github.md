```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.430 ns | 0.0128 ns | 0.0114 ns |  1.44 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.762 ns | 0.0106 ns | 0.0094 ns |  1.60 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.343 ns | 0.0049 ns | 0.0038 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.726 ns | 0.0261 ns | 0.0218 ns |  1.36 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 48.426 ns | 0.0301 ns | 0.0267 ns |  5.62 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.811 ns | 0.0379 ns | 0.0354 ns |  6.59 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.622 ns | 0.0186 ns | 0.0156 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.795 ns | 0.0035 ns | 0.0029 ns |  1.14 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.488 ns | 0.6730 ns | 0.0369 ns |  1.45 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.693 ns | 0.1488 ns | 0.0082 ns |  1.59 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.343 ns | 0.0797 ns | 0.0044 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.928 ns | 0.2387 ns | 0.0131 ns |  1.39 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 48.298 ns | 5.8030 ns | 0.3181 ns |  5.61 |    0.03 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.281 ns | 0.8959 ns | 0.0491 ns |  6.65 |    0.01 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.608 ns | 0.0338 ns | 0.0019 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.793 ns | 0.0622 ns | 0.0034 ns |  1.14 |    0.00 |         - |          NA |
