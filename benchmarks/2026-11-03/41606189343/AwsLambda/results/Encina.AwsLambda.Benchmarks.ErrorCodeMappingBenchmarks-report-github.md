```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 13.221 ns |  0.1927 ns | 0.1803 ns |  1.45 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 14.509 ns |  0.2393 ns | 0.2239 ns |  1.60 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 10.423 ns |  0.1687 ns | 0.1578 ns |  1.15 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 11.109 ns |  0.1220 ns | 0.1082 ns |  1.22 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 12.429 ns |  0.0905 ns | 0.0847 ns |  1.37 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 50.154 ns |  0.5375 ns | 0.5028 ns |  5.52 |    0.11 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 59.838 ns |  0.6271 ns | 0.5866 ns |  6.58 |    0.13 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  9.091 ns |  0.1743 ns | 0.1630 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 13.026 ns |  1.2372 ns | 0.0678 ns |  1.44 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 14.511 ns |  4.8098 ns | 0.2636 ns |  1.60 |    0.03 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 10.276 ns |  2.2980 ns | 0.1260 ns |  1.13 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.951 ns |  3.2682 ns | 0.1791 ns |  1.21 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 12.507 ns |  3.0565 ns | 0.1675 ns |  1.38 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 49.830 ns | 13.6357 ns | 0.7474 ns |  5.49 |    0.10 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 61.570 ns | 22.5966 ns | 1.2386 ns |  6.78 |    0.14 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  9.078 ns |  2.2460 ns | 0.1231 ns |  1.00 |    0.02 |         - |          NA |
