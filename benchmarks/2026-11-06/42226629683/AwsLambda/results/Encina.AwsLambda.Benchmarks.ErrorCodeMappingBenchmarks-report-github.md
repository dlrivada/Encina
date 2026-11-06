```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.66 ns |  0.324 ns | 0.522 ns |  1.44 |    0.07 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 16.09 ns |  0.353 ns | 0.760 ns |  1.59 |    0.09 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 11.56 ns |  0.263 ns | 0.351 ns |  1.14 |    0.05 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 12.09 ns |  0.269 ns | 0.434 ns |  1.19 |    0.06 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 13.77 ns |  0.307 ns | 0.441 ns |  1.36 |    0.06 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 55.30 ns |  1.106 ns | 1.656 ns |  5.45 |    0.25 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 66.87 ns |  1.351 ns | 2.965 ns |  6.59 |    0.37 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.16 ns |  0.235 ns | 0.352 ns |  1.00 |    0.05 |         - |          NA |
|                                 |            |                |             |             |          |           |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.43 ns | 15.833 ns | 0.868 ns |  1.34 |    0.09 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 15.96 ns |  4.620 ns | 0.253 ns |  1.48 |    0.06 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 11.63 ns | 12.542 ns | 0.687 ns |  1.08 |    0.07 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 11.94 ns | 10.272 ns | 0.563 ns |  1.11 |    0.06 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.15 ns |  6.789 ns | 0.372 ns |  1.32 |    0.06 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.26 ns | 22.920 ns | 1.256 ns |  5.14 |    0.22 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 68.37 ns | 38.278 ns | 2.098 ns |  6.35 |    0.29 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.77 ns |  8.778 ns | 0.481 ns |  1.00 |    0.05 |         - |          NA |
