```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.466 ns | 0.0425 ns | 0.0377 ns |  1.44 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.773 ns | 0.0131 ns | 0.0116 ns |  1.59 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.810 ns | 0.0159 ns | 0.0133 ns |  1.14 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.458 ns | 0.0216 ns | 0.0180 ns |  1.21 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.749 ns | 0.0226 ns | 0.0188 ns |  1.36 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.845 ns | 0.0647 ns | 0.0573 ns |  5.42 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.878 ns | 0.0838 ns | 0.0743 ns |  6.58 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.640 ns | 0.0105 ns | 0.0093 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.528 ns | 2.9388 ns | 0.1611 ns |  1.45 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.779 ns | 0.4067 ns | 0.0223 ns |  1.59 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.801 ns | 0.1282 ns | 0.0070 ns |  1.13 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.447 ns | 2.2460 ns | 0.1231 ns |  1.21 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 12.117 ns | 1.8327 ns | 0.1005 ns |  1.40 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.838 ns | 0.1841 ns | 0.0101 ns |  5.42 |    0.01 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.303 ns | 0.1971 ns | 0.0108 ns |  6.63 |    0.01 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.648 ns | 0.4087 ns | 0.0224 ns |  1.00 |    0.00 |         - |          NA |
