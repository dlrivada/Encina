```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.902 ns |  0.1099 ns | 0.1028 ns |  1.43 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.735 ns |  0.2369 ns | 0.4086 ns |  1.73 |    0.07 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.443 ns |  0.1421 ns | 0.1259 ns |  1.04 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  7.136 ns |  0.1655 ns | 0.2577 ns |  1.15 |    0.05 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.855 ns |  0.1478 ns | 0.1234 ns |  1.26 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 35.933 ns |  0.7397 ns | 0.9085 ns |  5.78 |    0.18 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 41.009 ns |  0.3526 ns | 0.2753 ns |  6.60 |    0.14 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.220 ns |  0.1469 ns | 0.1302 ns |  1.00 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.577 ns |  3.9355 ns | 0.2157 ns |  1.53 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 11.223 ns |  6.2905 ns | 0.3448 ns |  1.79 |    0.05 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.786 ns |  0.9279 ns | 0.0509 ns |  1.08 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  7.037 ns |  7.1136 ns | 0.3899 ns |  1.12 |    0.06 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.327 ns |  7.7190 ns | 0.4231 ns |  1.33 |    0.06 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 35.849 ns | 16.3894 ns | 0.8984 ns |  5.71 |    0.14 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 41.344 ns | 36.9006 ns | 2.0226 ns |  6.59 |    0.29 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.279 ns |  1.6691 ns | 0.0915 ns |  1.00 |    0.02 |         - |          NA |
