```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.359 ns |  0.0447 ns | 0.0418 ns |  1.51 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  9.309 ns |  0.0951 ns | 0.0890 ns |  1.68 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  5.859 ns |  0.1361 ns | 0.1273 ns |  1.06 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.496 ns |  0.1106 ns | 0.0980 ns |  1.17 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.274 ns |  0.1338 ns | 0.1252 ns |  1.31 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 29.727 ns |  0.4370 ns | 0.4088 ns |  5.36 |    0.13 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 35.622 ns |  0.5612 ns | 0.5250 ns |  6.43 |    0.16 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.545 ns |  0.1309 ns | 0.1224 ns |  1.00 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.078 ns |  0.4999 ns | 0.0274 ns |  1.41 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  8.910 ns |  2.7492 ns | 0.1507 ns |  1.56 |    0.04 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.623 ns |  1.7531 ns | 0.0961 ns |  1.16 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.407 ns |  3.9048 ns | 0.2140 ns |  1.12 |    0.04 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.687 ns |  2.0645 ns | 0.1132 ns |  1.35 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 30.421 ns |  1.5815 ns | 0.0867 ns |  5.32 |    0.13 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 37.017 ns | 12.3379 ns | 0.6763 ns |  6.48 |    0.19 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.717 ns |  2.9466 ns | 0.1615 ns |  1.00 |    0.03 |         - |          NA |
