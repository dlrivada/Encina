```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.629 ns |  0.2110 ns | 0.2817 ns |  1.52 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 11.164 ns |  0.2414 ns | 0.2683 ns |  1.76 |    0.05 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  7.290 ns |  0.1621 ns | 0.1991 ns |  1.15 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.883 ns |  0.0975 ns | 0.0865 ns |  1.24 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 36.842 ns |  0.7497 ns | 0.9207 ns |  5.80 |    0.16 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 41.201 ns |  0.6348 ns | 0.5938 ns |  6.49 |    0.12 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.351 ns |  0.0962 ns | 0.0803 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.860 ns |  0.1578 ns | 0.2264 ns |  1.08 |    0.04 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.708 ns |  1.5087 ns | 0.0827 ns |  1.40 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 11.125 ns |  4.3111 ns | 0.2363 ns |  1.61 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  7.117 ns |  1.7044 ns | 0.0934 ns |  1.03 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.127 ns |  1.4711 ns | 0.0806 ns |  1.18 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 36.481 ns | 19.8613 ns | 1.0887 ns |  5.28 |    0.14 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 40.917 ns | 10.7214 ns | 0.5877 ns |  5.92 |    0.08 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.913 ns |  0.9196 ns | 0.0504 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.658 ns |  1.8064 ns | 0.0990 ns |  0.96 |    0.01 |         - |          NA |
