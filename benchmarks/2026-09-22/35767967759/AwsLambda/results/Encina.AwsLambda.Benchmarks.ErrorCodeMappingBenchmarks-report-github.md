```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.452 ns | 0.0223 ns | 0.0198 ns |  1.45 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.937 ns | 0.0173 ns | 0.0162 ns |  1.62 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.850 ns | 0.0029 ns | 0.0023 ns |  1.14 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.354 ns | 0.0159 ns | 0.0141 ns |  1.20 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.729 ns | 0.0093 ns | 0.0078 ns |  1.36 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.786 ns | 0.0211 ns | 0.0187 ns |  5.43 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.833 ns | 0.0344 ns | 0.0305 ns |  6.60 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.614 ns | 0.0103 ns | 0.0091 ns |  1.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.411 ns | 0.0605 ns | 0.0033 ns |  1.30 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.727 ns | 0.3409 ns | 0.0187 ns |  1.44 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.806 ns | 0.2340 ns | 0.0128 ns |  1.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.340 ns | 0.0624 ns | 0.0034 ns |  1.08 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.872 ns | 0.5501 ns | 0.0302 ns |  1.24 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.800 ns | 0.3358 ns | 0.0184 ns |  4.90 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.377 ns | 0.2563 ns | 0.0140 ns |  6.01 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  9.552 ns | 0.0178 ns | 0.0010 ns |  1.00 |         - |          NA |
