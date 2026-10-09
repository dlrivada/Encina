```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 13.043 ns |  0.1487 ns | 0.1318 ns |  1.46 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.983 ns |  0.2551 ns | 0.2505 ns |  1.45 |    0.04 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 11.116 ns |  0.1346 ns | 0.1193 ns |  1.24 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.698 ns |  0.2298 ns | 0.2037 ns |  1.31 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 49.947 ns |  0.5321 ns | 0.4717 ns |  5.59 |    0.15 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 58.606 ns |  0.6431 ns | 0.5371 ns |  6.56 |    0.17 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.943 ns |  0.2185 ns | 0.2244 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.667 ns |  0.1442 ns | 0.1349 ns |  1.08 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 11.978 ns |  2.0246 ns | 0.1110 ns |  1.35 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.601 ns |  0.9619 ns | 0.0527 ns |  1.53 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 11.876 ns |  1.4719 ns | 0.0807 ns |  1.34 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.978 ns |  2.6238 ns | 0.1438 ns |  1.35 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 47.992 ns |  2.2743 ns | 0.1247 ns |  5.40 |    0.03 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 58.394 ns | 11.4755 ns | 0.6290 ns |  6.57 |    0.07 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.886 ns |  0.9395 ns | 0.0515 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.957 ns |  0.8247 ns | 0.0452 ns |  1.12 |    0.01 |         - |          NA |
