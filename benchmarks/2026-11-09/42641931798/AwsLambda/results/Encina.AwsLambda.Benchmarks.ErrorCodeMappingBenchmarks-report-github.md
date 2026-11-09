```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.76GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 13.436 ns |  0.2215 ns | 0.2071 ns | 13.344 ns |  1.41 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.886 ns |  0.0762 ns | 0.0636 ns | 13.866 ns |  1.46 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.891 ns |  0.0850 ns | 0.0664 ns |  9.860 ns |  1.04 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.274 ns |  0.0541 ns | 0.0451 ns | 10.256 ns |  1.08 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 10.674 ns |  0.1114 ns | 0.0870 ns | 10.639 ns |  1.12 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.533 ns |  0.9471 ns | 1.5824 ns | 45.714 ns |  4.89 |    0.16 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.865 ns |  1.0512 ns | 0.9319 ns | 56.349 ns |  5.97 |    0.09 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  9.519 ns |  0.0098 ns | 0.0082 ns |  9.516 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |            |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 13.684 ns |  6.4880 ns | 0.3556 ns | 13.667 ns |  1.37 |    0.07 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 14.808 ns | 28.9917 ns | 1.5891 ns | 13.905 ns |  1.48 |    0.15 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 10.492 ns | 10.3112 ns | 0.5652 ns | 10.225 ns |  1.05 |    0.07 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.940 ns | 19.0475 ns | 1.0441 ns | 10.409 ns |  1.09 |    0.10 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 10.866 ns |  0.4302 ns | 0.0236 ns | 10.870 ns |  1.08 |    0.05 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 44.994 ns |  0.2231 ns | 0.0122 ns | 44.996 ns |  4.49 |    0.20 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 56.894 ns |  1.6745 ns | 0.0918 ns | 56.847 ns |  5.68 |    0.26 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.035 ns |  9.6684 ns | 0.5300 ns |  9.985 ns |  1.00 |    0.06 |         - |          NA |
