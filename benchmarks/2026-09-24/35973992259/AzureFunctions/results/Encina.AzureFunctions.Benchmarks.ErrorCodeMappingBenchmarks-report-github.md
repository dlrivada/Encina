```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.431 ns | 0.0665 ns | 0.0622 ns |  1.44 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.563 ns | 0.1178 ns | 0.1102 ns |  1.46 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.525 ns | 0.1034 ns | 0.0967 ns |  1.22 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 10.973 ns | 0.0791 ns | 0.0701 ns |  1.27 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 47.529 ns | 0.7098 ns | 0.6640 ns |  5.51 |    0.13 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.524 ns | 0.6192 ns | 0.5792 ns |  6.56 |    0.14 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.623 ns | 0.1761 ns | 0.1648 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.415 ns | 0.1186 ns | 0.1110 ns |  1.09 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 11.546 ns | 1.5887 ns | 0.0871 ns |  1.34 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.132 ns | 0.2985 ns | 0.0164 ns |  1.53 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 11.545 ns | 0.5172 ns | 0.0283 ns |  1.34 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.469 ns | 2.7514 ns | 0.1508 ns |  1.33 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.377 ns | 1.8539 ns | 0.1016 ns |  5.39 |    0.10 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 56.493 ns | 4.5040 ns | 0.2469 ns |  6.57 |    0.12 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.606 ns | 3.2171 ns | 0.1763 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.620 ns | 2.5174 ns | 0.1380 ns |  1.12 |    0.02 |         - |          NA |
