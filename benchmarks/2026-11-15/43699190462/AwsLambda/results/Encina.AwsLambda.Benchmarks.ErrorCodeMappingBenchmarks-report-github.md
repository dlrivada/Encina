```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.43GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.696 ns |  0.2410 ns | 0.2254 ns |  1.37 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.184 ns |  0.2459 ns | 0.2300 ns |  1.43 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 10.412 ns |  0.1532 ns | 0.1358 ns |  1.13 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 11.607 ns |  0.2622 ns | 0.2693 ns |  1.26 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.931 ns |  0.1102 ns | 0.0977 ns |  1.29 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 47.896 ns |  0.3089 ns | 0.2889 ns |  5.18 |    0.08 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 59.270 ns |  1.2165 ns | 1.3017 ns |  6.41 |    0.16 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  9.241 ns |  0.1349 ns | 0.1262 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 11.939 ns |  4.3834 ns | 0.2403 ns |  1.33 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.704 ns |  7.2943 ns | 0.3998 ns |  1.52 |    0.06 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 10.474 ns |  2.0952 ns | 0.1148 ns |  1.16 |    0.04 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 11.685 ns |  1.6021 ns | 0.0878 ns |  1.30 |    0.04 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 12.205 ns |  0.9555 ns | 0.0524 ns |  1.36 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 47.969 ns |  2.7585 ns | 0.1512 ns |  5.33 |    0.16 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.837 ns | 11.3108 ns | 0.6200 ns |  6.43 |    0.20 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  9.007 ns |  5.6739 ns | 0.3110 ns |  1.00 |    0.04 |         - |          NA |
