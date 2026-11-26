```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.599 ns | 0.0721 ns | 0.0602 ns |  1.43 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.412 ns | 0.1709 ns | 0.1599 ns |  1.40 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 10.148 ns | 0.1587 ns | 0.1485 ns |  1.15 |    0.04 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 11.251 ns | 0.1579 ns | 0.1400 ns |  1.27 |    0.04 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.471 ns | 0.1850 ns | 0.1730 ns |  1.30 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.605 ns | 0.1846 ns | 0.1637 ns |  5.27 |    0.15 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 58.907 ns | 0.9521 ns | 0.8440 ns |  6.66 |    0.20 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.847 ns | 0.2116 ns | 0.2436 ns |  1.00 |    0.04 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 11.744 ns | 4.9755 ns | 0.2727 ns |  1.34 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.266 ns | 0.7512 ns | 0.0412 ns |  1.51 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 10.157 ns | 1.9418 ns | 0.1064 ns |  1.16 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 11.401 ns | 2.4310 ns | 0.1333 ns |  1.30 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.631 ns | 4.2184 ns | 0.2312 ns |  1.33 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 45.383 ns | 3.5616 ns | 0.1952 ns |  5.18 |    0.06 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.388 ns | 8.5852 ns | 0.4706 ns |  6.55 |    0.09 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.758 ns | 2.0156 ns | 0.1105 ns |  1.00 |    0.02 |         - |          NA |
