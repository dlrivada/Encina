```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.458 ns |  0.0299 ns | 0.0280 ns |  1.43 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.772 ns |  0.0225 ns | 0.0176 ns |  1.58 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.826 ns |  0.0414 ns | 0.0367 ns |  1.13 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.378 ns |  0.0716 ns | 0.0598 ns |  1.19 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.808 ns |  0.0488 ns | 0.0432 ns |  1.36 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.919 ns |  0.2026 ns | 0.1895 ns |  5.39 |    0.03 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 57.030 ns |  0.1900 ns | 0.1587 ns |  6.55 |    0.04 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.708 ns |  0.0484 ns | 0.0429 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.421 ns |  0.3195 ns | 0.0175 ns |  1.34 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.710 ns |  1.3379 ns | 0.0733 ns |  1.48 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.896 ns |  2.2393 ns | 0.1227 ns |  1.07 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.361 ns |  0.1426 ns | 0.0078 ns |  1.12 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.904 ns |  0.4109 ns | 0.0225 ns |  1.28 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 48.588 ns |  8.1192 ns | 0.4450 ns |  5.23 |    0.07 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.900 ns | 11.6917 ns | 0.6409 ns |  6.24 |    0.09 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  9.284 ns |  2.3062 ns | 0.1264 ns |  1.00 |    0.02 |         - |          NA |
