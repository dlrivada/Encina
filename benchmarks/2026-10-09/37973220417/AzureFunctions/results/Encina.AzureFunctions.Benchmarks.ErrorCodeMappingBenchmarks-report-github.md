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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 13.375 ns |  0.2912 ns | 0.2860 ns |  1.48 |    0.07 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.555 ns |  0.2522 ns | 0.2359 ns |  1.50 |    0.07 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 11.169 ns |  0.2503 ns | 0.2459 ns |  1.24 |    0.06 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.762 ns |  0.2533 ns | 0.2369 ns |  1.31 |    0.06 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 48.584 ns |  0.8141 ns | 0.7217 ns |  5.39 |    0.23 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 58.464 ns |  0.6278 ns | 0.5243 ns |  6.49 |    0.27 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  9.025 ns |  0.2187 ns | 0.3653 ns |  1.00 |    0.06 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.854 ns |  0.2308 ns | 0.2469 ns |  1.09 |    0.05 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.380 ns |  3.1747 ns | 0.1740 ns |  1.36 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.850 ns |  8.8925 ns | 0.4874 ns |  1.52 |    0.05 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 12.073 ns |  1.0198 ns | 0.0559 ns |  1.33 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 12.059 ns |  3.3777 ns | 0.1851 ns |  1.33 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 47.079 ns | 39.1186 ns | 2.1442 ns |  5.18 |    0.22 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 58.752 ns | 12.2894 ns | 0.6736 ns |  6.46 |    0.13 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  9.092 ns |  3.4122 ns | 0.1870 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.982 ns |  3.5312 ns | 0.1936 ns |  1.10 |    0.03 |         - |          NA |
