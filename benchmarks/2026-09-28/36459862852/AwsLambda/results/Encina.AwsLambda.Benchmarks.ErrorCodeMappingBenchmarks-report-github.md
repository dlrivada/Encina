```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.594 ns |  0.0722 ns | 0.0640 ns |  1.50 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.544 ns |  0.1600 ns | 0.1418 ns |  1.49 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.418 ns |  0.1525 ns | 0.1426 ns |  1.12 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.701 ns |  0.2144 ns | 0.2005 ns |  1.27 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.019 ns |  0.0618 ns | 0.0482 ns |  1.31 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 48.169 ns |  0.3086 ns | 0.2577 ns |  5.73 |    0.11 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 57.356 ns |  1.0655 ns | 0.9967 ns |  6.82 |    0.17 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.416 ns |  0.1768 ns | 0.1654 ns |  1.00 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 11.576 ns |  1.4311 ns | 0.0784 ns |  1.37 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.133 ns |  0.4486 ns | 0.0246 ns |  1.56 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.627 ns |  0.7866 ns | 0.0431 ns |  1.14 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 11.378 ns |  1.6721 ns | 0.0917 ns |  1.35 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.668 ns |  1.4882 ns | 0.0816 ns |  1.38 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.489 ns |  2.0852 ns | 0.1143 ns |  5.51 |    0.07 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.036 ns | 19.0524 ns | 1.0443 ns |  6.76 |    0.13 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.434 ns |  2.1630 ns | 0.1186 ns |  1.00 |    0.02 |         - |          NA |
