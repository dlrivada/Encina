```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.460 ns |  0.1813 ns | 0.1696 ns |  1.45 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  9.156 ns |  0.0794 ns | 0.0703 ns |  1.57 |    0.04 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.505 ns |  0.1340 ns | 0.1254 ns |  1.12 |    0.04 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.238 ns |  0.1153 ns | 0.0963 ns |  1.24 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 29.761 ns |  0.5277 ns | 0.4678 ns |  5.11 |    0.16 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 37.405 ns |  0.7570 ns | 0.9843 ns |  6.42 |    0.24 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.831 ns |  0.1280 ns | 0.1572 ns |  1.00 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.078 ns |  0.1257 ns | 0.1114 ns |  1.04 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.374 ns |  1.7270 ns | 0.0947 ns |  1.33 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  9.585 ns |  2.2225 ns | 0.1218 ns |  1.52 |    0.05 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.300 ns |  1.3340 ns | 0.0731 ns |  1.00 |    0.03 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.947 ns |  3.1699 ns | 0.1738 ns |  1.26 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 31.498 ns |  7.3794 ns | 0.4045 ns |  5.00 |    0.16 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 38.829 ns | 17.7567 ns | 0.9733 ns |  6.16 |    0.22 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.308 ns |  3.8258 ns | 0.2097 ns |  1.00 |    0.04 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.313 ns |  2.4871 ns | 0.1363 ns |  1.00 |    0.03 |         - |          NA |
