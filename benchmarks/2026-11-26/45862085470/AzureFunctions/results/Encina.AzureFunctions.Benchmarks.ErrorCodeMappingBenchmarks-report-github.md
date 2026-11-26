```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  7.945 ns |  0.0526 ns | 0.0492 ns |  1.46 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  8.974 ns |  0.1257 ns | 0.1114 ns |  1.65 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.289 ns |  0.0484 ns | 0.0378 ns |  1.15 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.115 ns |  0.0486 ns | 0.0454 ns |  1.31 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 29.577 ns |  0.4821 ns | 0.4274 ns |  5.43 |    0.08 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 35.637 ns |  0.2243 ns | 0.2098 ns |  6.54 |    0.06 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.448 ns |  0.0429 ns | 0.0380 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  5.819 ns |  0.0607 ns | 0.0568 ns |  1.07 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  7.951 ns |  1.4004 ns | 0.0768 ns |  1.48 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  8.773 ns |  0.2261 ns | 0.0124 ns |  1.64 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.244 ns |  1.8536 ns | 0.1016 ns |  1.16 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.491 ns |  0.3100 ns | 0.0170 ns |  1.40 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 28.912 ns |  2.7481 ns | 0.1506 ns |  5.39 |    0.04 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 36.471 ns | 32.9283 ns | 1.8049 ns |  6.80 |    0.29 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.361 ns |  0.5594 ns | 0.0307 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  5.843 ns |  1.1095 ns | 0.0608 ns |  1.09 |    0.01 |         - |          NA |
