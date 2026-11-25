```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.81GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.60 ns |  0.085 ns | 0.075 ns |  1.39 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 15.42 ns |  0.055 ns | 0.051 ns |  1.46 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 12.64 ns |  0.192 ns | 0.161 ns |  1.20 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 13.46 ns |  0.114 ns | 0.107 ns |  1.28 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 54.04 ns |  0.114 ns | 0.107 ns |  5.13 |    0.09 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 66.50 ns |  1.031 ns | 0.964 ns |  6.32 |    0.14 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.53 ns |  0.199 ns | 0.186 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 11.49 ns |  0.141 ns | 0.132 ns |  1.09 |    0.02 |         - |          NA |
|                                 |            |                |             |             |          |           |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 13.47 ns |  0.662 ns | 0.036 ns |  1.32 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 15.36 ns |  0.794 ns | 0.044 ns |  1.51 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 12.94 ns |  0.630 ns | 0.035 ns |  1.27 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 13.72 ns |  1.795 ns | 0.098 ns |  1.35 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 52.07 ns |  8.785 ns | 0.482 ns |  5.11 |    0.07 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 64.67 ns | 13.567 ns | 0.744 ns |  6.35 |    0.10 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.18 ns |  2.451 ns | 0.134 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 11.83 ns |  1.731 ns | 0.095 ns |  1.16 |    0.02 |         - |          NA |
