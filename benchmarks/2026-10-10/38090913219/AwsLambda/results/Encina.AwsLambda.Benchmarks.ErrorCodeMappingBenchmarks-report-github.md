```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.717 ns |  0.1479 ns | 0.1311 ns |  1.50 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.711 ns |  0.1251 ns | 0.1170 ns |  1.50 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.517 ns |  0.0904 ns | 0.0802 ns |  1.12 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.845 ns |  0.2409 ns | 0.2253 ns |  1.28 |    0.04 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.275 ns |  0.1195 ns | 0.1118 ns |  1.33 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 48.885 ns |  0.4722 ns | 0.3943 ns |  5.77 |    0.17 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 57.716 ns |  0.6541 ns | 0.6118 ns |  6.81 |    0.20 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.485 ns |  0.2065 ns | 0.2458 ns |  1.00 |    0.04 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.996 ns | 12.0500 ns | 0.6605 ns |  1.42 |    0.06 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.335 ns |  2.2654 ns | 0.1242 ns |  1.46 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.828 ns |  1.9471 ns | 0.1067 ns |  1.07 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 12.241 ns | 10.0360 ns | 0.5501 ns |  1.34 |    0.05 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.493 ns |  2.8120 ns | 0.1541 ns |  1.25 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.876 ns |  6.1839 ns | 0.3390 ns |  5.12 |    0.04 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.795 ns |  5.3063 ns | 0.2909 ns |  6.31 |    0.04 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  9.160 ns |  0.8332 ns | 0.0457 ns |  1.00 |    0.01 |         - |          NA |
