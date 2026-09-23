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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.445 ns | 0.0178 ns | 0.0167 ns |  1.40 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.838 ns | 0.0083 ns | 0.0065 ns |  1.56 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.345 ns | 0.0124 ns | 0.0116 ns |  1.17 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.740 ns | 0.0272 ns | 0.0227 ns |  1.32 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.803 ns | 0.0327 ns | 0.0256 ns |  5.28 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 57.238 ns | 0.0758 ns | 0.0672 ns |  6.46 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.865 ns | 0.0223 ns | 0.0187 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.808 ns | 0.0276 ns | 0.0230 ns |  1.11 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.523 ns | 2.3085 ns | 0.1265 ns |  1.45 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.709 ns | 0.1458 ns | 0.0080 ns |  1.59 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.375 ns | 1.0715 ns | 0.0587 ns |  1.21 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.866 ns | 0.3769 ns | 0.0207 ns |  1.38 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.879 ns | 1.7904 ns | 0.0981 ns |  5.44 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.419 ns | 0.3976 ns | 0.0218 ns |  6.67 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.610 ns | 0.1737 ns | 0.0095 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.789 ns | 0.0460 ns | 0.0025 ns |  1.14 |         - |          NA |
