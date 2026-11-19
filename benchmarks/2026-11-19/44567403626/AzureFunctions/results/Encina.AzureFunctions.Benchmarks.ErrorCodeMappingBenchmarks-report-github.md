```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  7.919 ns |  0.0848 ns | 0.0708 ns |  1.45 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  8.914 ns |  0.1733 ns | 0.1536 ns |  1.64 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.152 ns |  0.0334 ns | 0.0296 ns |  1.13 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.416 ns |  0.1692 ns | 0.2635 ns |  1.36 |    0.05 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 28.768 ns |  0.1578 ns | 0.1476 ns |  5.28 |    0.04 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 34.863 ns |  0.3267 ns | 0.2728 ns |  6.40 |    0.06 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.444 ns |  0.0415 ns | 0.0346 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  5.769 ns |  0.0472 ns | 0.0394 ns |  1.06 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  7.952 ns |  0.6606 ns | 0.0362 ns |  1.41 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  8.829 ns |  2.9007 ns | 0.1590 ns |  1.57 |    0.04 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.161 ns |  0.6083 ns | 0.0333 ns |  1.10 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.461 ns |  0.5515 ns | 0.0302 ns |  1.33 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 32.629 ns | 25.7028 ns | 1.4089 ns |  5.80 |    0.24 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 35.294 ns |  2.9588 ns | 0.1622 ns |  6.27 |    0.11 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.627 ns |  2.0683 ns | 0.1134 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  5.769 ns |  0.3554 ns | 0.0195 ns |  1.03 |    0.02 |         - |          NA |
