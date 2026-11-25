```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 11.412 ns | 0.0563 ns | 0.0527 ns |  1.39 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.865 ns | 0.1457 ns | 0.1363 ns |  1.57 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  9.051 ns | 0.0167 ns | 0.0157 ns |  1.10 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  9.973 ns | 0.0147 ns | 0.0130 ns |  1.21 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 41.414 ns | 0.0833 ns | 0.0779 ns |  5.05 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 50.259 ns | 0.1740 ns | 0.1627 ns |  6.12 |    0.03 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.209 ns | 0.0384 ns | 0.0359 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.832 ns | 0.0248 ns | 0.0220 ns |  1.08 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 11.412 ns | 0.8850 ns | 0.0485 ns |  1.38 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.188 ns | 1.5194 ns | 0.0833 ns |  1.60 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  9.312 ns | 0.2720 ns | 0.0149 ns |  1.13 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 10.146 ns | 0.0529 ns | 0.0029 ns |  1.23 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 41.522 ns | 0.9404 ns | 0.0515 ns |  5.04 |    0.01 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 49.703 ns | 4.3534 ns | 0.2386 ns |  6.03 |    0.03 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.241 ns | 0.4057 ns | 0.0222 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.060 ns | 0.2071 ns | 0.0114 ns |  1.10 |    0.00 |         - |          NA |
