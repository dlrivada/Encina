```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.069 ns | 0.1652 ns | 0.1545 ns |  1.44 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.455 ns | 0.2013 ns | 0.1977 ns |  1.66 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.596 ns | 0.1456 ns | 0.1430 ns |  1.05 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.736 ns | 0.1513 ns | 0.1415 ns |  1.07 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.504 ns | 0.1384 ns | 0.1294 ns |  1.19 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 36.670 ns | 0.5273 ns | 0.4932 ns |  5.84 |    0.09 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 40.212 ns | 0.3109 ns | 0.2756 ns |  6.40 |    0.07 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.282 ns | 0.0635 ns | 0.0530 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.603 ns | 1.0205 ns | 0.0559 ns |  1.52 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.973 ns | 5.5477 ns | 0.3041 ns |  1.74 |    0.04 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.751 ns | 0.7362 ns | 0.0404 ns |  1.07 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.941 ns | 2.0610 ns | 0.1130 ns |  1.10 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.150 ns | 1.4675 ns | 0.0804 ns |  1.29 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 34.995 ns | 2.7745 ns | 0.1521 ns |  5.55 |    0.03 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 40.007 ns | 4.5332 ns | 0.2485 ns |  6.35 |    0.04 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.305 ns | 0.5060 ns | 0.0277 ns |  1.00 |    0.01 |         - |          NA |
