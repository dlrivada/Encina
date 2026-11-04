```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.444 ns | 0.0265 ns | 0.0207 ns |  1.42 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.760 ns | 0.0127 ns | 0.0106 ns |  1.57 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.805 ns | 0.0155 ns | 0.0121 ns |  1.12 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 11.211 ns | 0.0619 ns | 0.0548 ns |  1.28 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.745 ns | 0.0250 ns | 0.0209 ns |  1.34 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 47.754 ns | 0.3750 ns | 0.3508 ns |  5.45 |    0.04 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.803 ns | 0.0348 ns | 0.0326 ns |  6.48 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.770 ns | 0.0074 ns | 0.0066 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.462 ns | 0.4858 ns | 0.0266 ns |  1.44 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.797 ns | 0.2226 ns | 0.0122 ns |  1.60 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.781 ns | 0.0412 ns | 0.0023 ns |  1.13 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.414 ns | 0.1640 ns | 0.0090 ns |  1.21 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.882 ns | 0.3478 ns | 0.0191 ns |  1.38 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.852 ns | 1.0113 ns | 0.0554 ns |  5.43 |    0.01 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.282 ns | 0.2249 ns | 0.0123 ns |  6.64 |    0.01 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.633 ns | 0.2694 ns | 0.0148 ns |  1.00 |    0.00 |         - |          NA |
