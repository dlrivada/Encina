```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  7.958 ns |  0.1052 ns | 0.0984 ns |  1.41 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  9.038 ns |  0.2036 ns | 0.2263 ns |  1.60 |    0.06 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.040 ns |  0.1402 ns | 0.2010 ns |  1.07 |    0.05 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.300 ns |  0.1448 ns | 0.1487 ns |  1.12 |    0.04 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.233 ns |  0.0784 ns | 0.0695 ns |  1.28 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 29.019 ns |  0.2400 ns | 0.1874 ns |  5.14 |    0.14 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 35.658 ns |  0.7187 ns | 0.9345 ns |  6.31 |    0.24 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.651 ns |  0.1354 ns | 0.1560 ns |  1.00 |    0.04 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.506 ns | 10.2736 ns | 0.5631 ns |  1.47 |    0.09 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  8.938 ns |  1.3228 ns | 0.0725 ns |  1.55 |    0.04 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  5.842 ns |  0.4791 ns | 0.0263 ns |  1.01 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.538 ns |  4.9264 ns | 0.2700 ns |  1.13 |    0.05 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.665 ns |  2.8455 ns | 0.1560 ns |  1.33 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 29.489 ns |  7.2884 ns | 0.3995 ns |  5.11 |    0.14 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 36.245 ns | 18.6966 ns | 1.0248 ns |  6.28 |    0.22 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.778 ns |  3.1238 ns | 0.1712 ns |  1.00 |    0.04 |         - |          NA |
