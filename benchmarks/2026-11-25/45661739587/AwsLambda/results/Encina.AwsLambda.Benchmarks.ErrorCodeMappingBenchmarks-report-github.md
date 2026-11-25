```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.251 ns |  0.1821 ns | 0.1704 ns |  1.49 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  9.180 ns |  0.1881 ns | 0.1759 ns |  1.66 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.065 ns |  0.1248 ns | 0.1106 ns |  1.10 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.426 ns |  0.0719 ns | 0.0673 ns |  1.16 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.581 ns |  0.1240 ns | 0.1100 ns |  1.37 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 30.493 ns |  0.5818 ns | 0.5442 ns |  5.52 |    0.13 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 36.844 ns |  0.2964 ns | 0.2773 ns |  6.67 |    0.12 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.523 ns |  0.1001 ns | 0.0936 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.311 ns |  3.9841 ns | 0.2184 ns |  1.27 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  9.062 ns |  2.4540 ns | 0.1345 ns |  1.38 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.461 ns |  5.5929 ns | 0.3066 ns |  0.99 |    0.04 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.379 ns |  1.3428 ns | 0.0736 ns |  0.97 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.598 ns |  0.2059 ns | 0.0113 ns |  1.16 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 29.990 ns | 18.9765 ns | 1.0402 ns |  4.58 |    0.14 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 36.402 ns |  6.9194 ns | 0.3793 ns |  5.56 |    0.05 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.549 ns |  0.4791 ns | 0.0263 ns |  1.00 |    0.00 |         - |          NA |
