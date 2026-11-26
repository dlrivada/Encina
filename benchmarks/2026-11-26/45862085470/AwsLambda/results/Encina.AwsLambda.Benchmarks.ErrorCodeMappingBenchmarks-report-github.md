```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.485 ns | 0.0496 ns | 0.0440 ns |  1.44 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.773 ns | 0.0219 ns | 0.0205 ns |  1.59 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.844 ns | 0.0728 ns | 0.0645 ns |  1.14 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.490 ns | 0.1284 ns | 0.1201 ns |  1.21 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 12.080 ns | 0.0790 ns | 0.0739 ns |  1.40 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.814 ns | 0.0420 ns | 0.0328 ns |  5.41 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.956 ns | 0.1606 ns | 0.1424 ns |  6.58 |    0.03 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.653 ns | 0.0332 ns | 0.0277 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.520 ns | 0.0304 ns | 0.0017 ns |  1.44 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.692 ns | 0.3793 ns | 0.0208 ns |  1.57 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.960 ns | 2.4121 ns | 0.1322 ns |  1.14 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.477 ns | 2.9006 ns | 0.1590 ns |  1.20 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.899 ns | 0.7532 ns | 0.0413 ns |  1.36 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.940 ns | 3.8409 ns | 0.2105 ns |  5.38 |    0.04 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 59.749 ns | 6.2220 ns | 0.3410 ns |  6.85 |    0.05 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.725 ns | 0.9999 ns | 0.0548 ns |  1.00 |    0.01 |         - |          NA |
