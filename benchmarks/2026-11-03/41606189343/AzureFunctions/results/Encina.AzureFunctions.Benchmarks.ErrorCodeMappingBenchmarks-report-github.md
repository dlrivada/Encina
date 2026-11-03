```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.133 ns |  0.2009 ns | 0.2541 ns |  1.48 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.665 ns |  0.2295 ns | 0.2903 ns |  1.73 |    0.06 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.738 ns |  0.1561 ns | 0.1533 ns |  1.09 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  9.140 ns |  0.1043 ns | 0.0871 ns |  1.49 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 34.574 ns |  0.6960 ns | 0.6510 ns |  5.62 |    0.16 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 38.850 ns |  0.5064 ns | 0.4737 ns |  6.31 |    0.15 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.156 ns |  0.1391 ns | 0.1302 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.538 ns |  0.1529 ns | 0.1502 ns |  1.06 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.758 ns |  7.5398 ns | 0.4133 ns |  1.61 |    0.06 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.627 ns |  6.2740 ns | 0.3439 ns |  1.75 |    0.05 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.643 ns |  0.7743 ns | 0.0424 ns |  1.09 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.930 ns | 15.1194 ns | 0.8287 ns |  1.47 |    0.12 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 34.156 ns | 25.5772 ns | 1.4020 ns |  5.62 |    0.21 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 39.807 ns | 17.7765 ns | 0.9744 ns |  6.55 |    0.16 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.078 ns |  1.7216 ns | 0.0944 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.616 ns |  4.3214 ns | 0.2369 ns |  1.09 |    0.04 |         - |          NA |
