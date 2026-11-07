```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.34 ns |  0.098 ns | 0.076 ns |  1.35 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.41 ns |  0.041 ns | 0.037 ns |  1.52 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.07 ns |  0.082 ns | 0.073 ns |  1.08 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.15 ns |  0.068 ns | 0.060 ns |  1.17 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.13 ns |  0.071 ns | 0.063 ns |  1.25 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 65.74 ns |  0.335 ns | 0.297 ns |  5.44 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 80.09 ns |  0.394 ns | 0.369 ns |  6.63 |    0.03 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 12.09 ns |  0.019 ns | 0.017 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |           |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 16.65 ns |  0.212 ns | 0.012 ns |  1.37 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 18.53 ns |  0.226 ns | 0.012 ns |  1.53 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.16 ns |  0.074 ns | 0.004 ns |  1.08 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.83 ns |  2.173 ns | 0.119 ns |  1.22 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.62 ns |  1.316 ns | 0.072 ns |  1.29 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 68.27 ns |  0.515 ns | 0.028 ns |  5.62 |    0.01 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 79.72 ns | 10.514 ns | 0.576 ns |  6.57 |    0.04 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 12.14 ns |  0.573 ns | 0.031 ns |  1.00 |    0.00 |         - |          NA |
