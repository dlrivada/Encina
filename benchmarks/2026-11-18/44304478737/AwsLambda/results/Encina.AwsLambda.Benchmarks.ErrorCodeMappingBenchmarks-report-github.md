```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  7.951 ns |  0.0835 ns | 0.0697 ns |  1.42 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  8.866 ns |  0.0759 ns | 0.0673 ns |  1.58 |    0.05 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  5.931 ns |  0.1011 ns | 0.0946 ns |  1.06 |    0.04 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.233 ns |  0.1186 ns | 0.1109 ns |  1.11 |    0.04 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.241 ns |  0.0371 ns | 0.0329 ns |  1.29 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 29.485 ns |  0.4839 ns | 0.4526 ns |  5.25 |    0.19 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 35.754 ns |  0.6805 ns | 0.6684 ns |  6.37 |    0.24 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.620 ns |  0.1339 ns | 0.1877 ns |  1.00 |    0.05 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.044 ns |  3.6618 ns | 0.2007 ns |  1.42 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.204 ns |  3.1881 ns | 0.1748 ns |  1.80 |    0.05 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  5.809 ns |  0.2531 ns | 0.0139 ns |  1.02 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.411 ns |  1.6354 ns | 0.0896 ns |  1.13 |    0.03 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.092 ns |  1.0875 ns | 0.0596 ns |  1.42 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 29.982 ns | 34.8075 ns | 1.9079 ns |  5.28 |    0.32 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 35.392 ns |  4.8069 ns | 0.2635 ns |  6.23 |    0.16 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.684 ns |  3.0503 ns | 0.1672 ns |  1.00 |    0.04 |         - |          NA |
