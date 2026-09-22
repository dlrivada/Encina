```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.443 ns | 0.0137 ns | 0.0128 ns |  1.44 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.771 ns | 0.0293 ns | 0.0244 ns |  1.60 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.800 ns | 0.0055 ns | 0.0043 ns |  1.14 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.381 ns | 0.0146 ns | 0.0122 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.728 ns | 0.0089 ns | 0.0083 ns |  1.36 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 47.142 ns | 0.0391 ns | 0.0305 ns |  5.47 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.798 ns | 0.0285 ns | 0.0223 ns |  6.58 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.626 ns | 0.0085 ns | 0.0066 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.429 ns | 0.1757 ns | 0.0096 ns |  1.44 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.709 ns | 0.1771 ns | 0.0097 ns |  1.59 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.795 ns | 0.2088 ns | 0.0114 ns |  1.13 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.353 ns | 0.2956 ns | 0.0162 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.905 ns | 0.2456 ns | 0.0135 ns |  1.38 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.779 ns | 0.4334 ns | 0.0238 ns |  5.42 |    0.01 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.526 ns | 4.0003 ns | 0.2193 ns |  6.66 |    0.02 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.636 ns | 0.2377 ns | 0.0130 ns |  1.00 |    0.00 |         - |          NA |
