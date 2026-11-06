```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.39 ns | 0.064 ns | 0.053 ns |  1.35 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.42 ns | 0.033 ns | 0.029 ns |  1.52 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.12 ns | 0.068 ns | 0.064 ns |  1.16 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.14 ns | 0.034 ns | 0.030 ns |  1.25 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 65.57 ns | 0.256 ns | 0.239 ns |  5.41 |    0.04 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 80.74 ns | 0.408 ns | 0.382 ns |  6.66 |    0.05 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 12.13 ns | 0.077 ns | 0.068 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.12 ns | 0.039 ns | 0.036 ns |  1.08 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 16.67 ns | 0.693 ns | 0.038 ns |  1.36 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 18.59 ns | 1.666 ns | 0.091 ns |  1.52 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.22 ns | 0.832 ns | 0.046 ns |  1.16 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.95 ns | 2.760 ns | 0.151 ns |  1.22 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 68.02 ns | 2.488 ns | 0.136 ns |  5.55 |    0.07 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 79.40 ns | 6.792 ns | 0.372 ns |  6.48 |    0.08 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 12.26 ns | 3.021 ns | 0.166 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.07 ns | 0.855 ns | 0.047 ns |  1.07 |    0.01 |         - |          NA |
