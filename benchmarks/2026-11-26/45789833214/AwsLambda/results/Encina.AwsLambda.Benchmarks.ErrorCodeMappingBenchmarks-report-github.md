```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.476 ns | 0.0493 ns | 0.0461 ns |  1.44 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.838 ns | 0.0929 ns | 0.0823 ns |  1.60 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.835 ns | 0.0501 ns | 0.0468 ns |  1.14 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.397 ns | 0.0517 ns | 0.0458 ns |  1.20 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.767 ns | 0.0410 ns | 0.0383 ns |  1.36 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.889 ns | 0.1556 ns | 0.1299 ns |  5.42 |    0.03 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 57.031 ns | 0.2278 ns | 0.2131 ns |  6.59 |    0.04 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.655 ns | 0.0585 ns | 0.0488 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.444 ns | 0.3274 ns | 0.0179 ns |  1.44 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.706 ns | 0.2740 ns | 0.0150 ns |  1.58 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.773 ns | 1.3202 ns | 0.0724 ns |  1.13 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.363 ns | 0.7926 ns | 0.0434 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 12.120 ns | 1.4689 ns | 0.0805 ns |  1.40 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 47.443 ns | 3.4722 ns | 0.1903 ns |  5.48 |    0.02 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.430 ns | 0.8528 ns | 0.0467 ns |  6.64 |    0.01 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.654 ns | 0.2459 ns | 0.0135 ns |  1.00 |    0.00 |         - |          NA |
