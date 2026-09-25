```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.551 ns | 0.1473 ns | 0.1378 ns |  1.47 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.285 ns | 0.0702 ns | 0.0657 ns |  1.43 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.668 ns | 0.1581 ns | 0.1321 ns |  1.25 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 10.986 ns | 0.0783 ns | 0.0732 ns |  1.28 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.367 ns | 0.1988 ns | 0.1660 ns |  5.42 |    0.11 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 57.266 ns | 0.8851 ns | 0.8280 ns |  6.69 |    0.17 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.565 ns | 0.2059 ns | 0.1825 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.412 ns | 0.1498 ns | 0.1401 ns |  1.10 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.550 ns | 1.0887 ns | 0.0597 ns |  1.46 |    0.06 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.096 ns | 0.0434 ns | 0.0024 ns |  1.52 |    0.06 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 11.596 ns | 2.8370 ns | 0.1555 ns |  1.35 |    0.05 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.340 ns | 4.2211 ns | 0.2314 ns |  1.32 |    0.06 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.317 ns | 0.5583 ns | 0.0306 ns |  5.39 |    0.21 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 56.389 ns | 9.4955 ns | 0.5205 ns |  6.56 |    0.26 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.611 ns | 6.7332 ns | 0.3691 ns |  1.00 |    0.05 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.691 ns | 0.7750 ns | 0.0425 ns |  1.13 |    0.04 |         - |          NA |
