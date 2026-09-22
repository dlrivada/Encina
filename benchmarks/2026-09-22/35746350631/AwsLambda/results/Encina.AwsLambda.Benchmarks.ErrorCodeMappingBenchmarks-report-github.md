```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.680 ns |  0.1727 ns | 0.1531 ns |  1.56 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  9.046 ns |  0.2001 ns | 0.2457 ns |  1.62 |    0.06 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  5.899 ns |  0.1127 ns | 0.1385 ns |  1.06 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.433 ns |  0.1479 ns | 0.2167 ns |  1.15 |    0.05 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.653 ns |  0.1748 ns | 0.2722 ns |  1.37 |    0.06 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 31.319 ns |  0.2932 ns | 0.2743 ns |  5.61 |    0.14 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 38.394 ns |  0.5684 ns | 0.5317 ns |  6.88 |    0.19 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.585 ns |  0.1280 ns | 0.1369 ns |  1.00 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.603 ns |  1.2034 ns | 0.0660 ns |  1.54 |    0.08 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  9.530 ns |  0.2094 ns | 0.0115 ns |  1.71 |    0.09 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.459 ns |  6.5361 ns | 0.3583 ns |  1.16 |    0.08 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.795 ns |  1.1816 ns | 0.0648 ns |  1.22 |    0.07 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.167 ns |  4.6011 ns | 0.2522 ns |  1.47 |    0.09 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 31.520 ns |  4.3117 ns | 0.2363 ns |  5.65 |    0.30 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 38.985 ns | 13.4304 ns | 0.7362 ns |  6.99 |    0.39 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.588 ns |  6.4523 ns | 0.3537 ns |  1.00 |    0.08 |         - |          NA |
