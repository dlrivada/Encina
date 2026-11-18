```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.09 ns | 0.193 ns | 0.181 ns |  1.43 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.93 ns | 0.016 ns | 0.013 ns |  1.60 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.74 ns | 0.060 ns | 0.047 ns |  1.30 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.08 ns | 0.142 ns | 0.133 ns |  1.43 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 57.05 ns | 0.744 ns | 0.696 ns |  5.39 |    0.07 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.93 ns | 0.500 ns | 0.468 ns |  6.70 |    0.05 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.58 ns | 0.060 ns | 0.053 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.22 ns | 0.069 ns | 0.061 ns |  1.15 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.98 ns | 3.235 ns | 0.177 ns |  1.44 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.37 ns | 0.232 ns | 0.013 ns |  1.57 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.51 ns | 3.106 ns | 0.170 ns |  1.30 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.69 ns | 0.848 ns | 0.046 ns |  1.41 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.41 ns | 0.757 ns | 0.041 ns |  5.32 |    0.04 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 71.02 ns | 0.621 ns | 0.034 ns |  6.81 |    0.05 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.43 ns | 1.597 ns | 0.088 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.42 ns | 0.668 ns | 0.037 ns |  1.19 |    0.01 |         - |          NA |
