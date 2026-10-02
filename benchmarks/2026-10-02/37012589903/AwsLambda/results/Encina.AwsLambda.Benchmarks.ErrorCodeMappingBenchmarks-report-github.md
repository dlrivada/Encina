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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.605 ns | 0.1505 ns | 0.1408 ns |  1.45 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 14.605 ns | 0.1792 ns | 0.1676 ns |  1.45 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 11.149 ns | 0.1796 ns | 0.1680 ns |  1.10 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 12.348 ns | 0.1184 ns | 0.1107 ns |  1.22 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 12.975 ns | 0.1697 ns | 0.1588 ns |  1.29 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 55.874 ns | 0.4825 ns | 0.4513 ns |  5.54 |    0.13 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 66.378 ns | 0.8104 ns | 0.7184 ns |  6.58 |    0.16 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.095 ns | 0.2455 ns | 0.2296 ns |  1.00 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.625 ns | 0.6284 ns | 0.0344 ns |  1.47 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 15.341 ns | 0.1339 ns | 0.0073 ns |  1.54 |    0.03 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 11.505 ns | 2.2270 ns | 0.1221 ns |  1.15 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 12.734 ns | 2.7984 ns | 0.1534 ns |  1.28 |    0.03 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 13.257 ns | 1.9048 ns | 0.1044 ns |  1.33 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.959 ns | 7.6039 ns | 0.4168 ns |  5.61 |    0.12 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 64.298 ns | 3.5719 ns | 0.1958 ns |  6.45 |    0.13 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  9.978 ns | 4.4212 ns | 0.2423 ns |  1.00 |    0.03 |         - |          NA |
