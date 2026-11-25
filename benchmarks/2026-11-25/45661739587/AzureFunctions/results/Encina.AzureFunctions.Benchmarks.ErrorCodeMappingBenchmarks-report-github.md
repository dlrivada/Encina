```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.02 ns | 0.120 ns | 0.112 ns |  1.37 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 15.95 ns | 0.105 ns | 0.098 ns |  1.45 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.14 ns | 0.301 ns | 0.335 ns |  1.20 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 13.98 ns | 0.179 ns | 0.168 ns |  1.27 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 55.86 ns | 0.167 ns | 0.148 ns |  5.09 |    0.08 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 69.28 ns | 1.024 ns | 0.958 ns |  6.31 |    0.13 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.98 ns | 0.178 ns | 0.166 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 11.93 ns | 0.188 ns | 0.176 ns |  1.09 |    0.02 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.04 ns | 4.447 ns | 0.244 ns |  1.35 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 15.95 ns | 0.960 ns | 0.053 ns |  1.54 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.53 ns | 1.971 ns | 0.108 ns |  1.30 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.00 ns | 2.227 ns | 0.122 ns |  1.35 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 53.84 ns | 4.871 ns | 0.267 ns |  5.19 |    0.05 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 65.60 ns | 2.722 ns | 0.149 ns |  6.32 |    0.06 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.37 ns | 2.073 ns | 0.114 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.14 ns | 0.979 ns | 0.054 ns |  1.17 |    0.01 |         - |          NA |
