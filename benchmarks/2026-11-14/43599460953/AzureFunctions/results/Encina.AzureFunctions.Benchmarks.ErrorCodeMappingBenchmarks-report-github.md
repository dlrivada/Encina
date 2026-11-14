```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.403 ns |  0.1866 ns | 0.1558 ns |  1.44 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  9.172 ns |  0.1822 ns | 0.1615 ns |  1.58 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.528 ns |  0.1475 ns | 0.2019 ns |  1.12 |    0.04 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.679 ns |  0.1029 ns | 0.0859 ns |  1.32 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 30.709 ns |  0.5153 ns | 0.4303 ns |  5.28 |    0.09 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 37.963 ns |  0.5283 ns | 0.4941 ns |  6.53 |    0.10 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.816 ns |  0.0679 ns | 0.0567 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.356 ns |  0.1508 ns | 0.1411 ns |  1.09 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.314 ns |  1.2869 ns | 0.0705 ns |  1.45 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  9.520 ns |  2.4642 ns | 0.1351 ns |  1.66 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.720 ns |  0.3820 ns | 0.0209 ns |  1.17 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.210 ns |  4.0120 ns | 0.2199 ns |  1.43 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 30.602 ns |  2.1657 ns | 0.1187 ns |  5.32 |    0.06 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 38.143 ns | 16.5295 ns | 0.9060 ns |  6.63 |    0.15 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.750 ns |  1.2475 ns | 0.0684 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.339 ns |  1.0062 ns | 0.0552 ns |  1.10 |    0.01 |         - |          NA |
