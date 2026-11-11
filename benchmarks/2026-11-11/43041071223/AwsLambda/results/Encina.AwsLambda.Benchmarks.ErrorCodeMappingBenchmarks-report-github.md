```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.93GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 11.029 ns |  0.1648 ns | 0.1541 ns |  1.40 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.207 ns |  0.0789 ns | 0.0699 ns |  1.55 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.482 ns |  0.1064 ns | 0.0995 ns |  1.07 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  8.619 ns |  0.0416 ns | 0.0389 ns |  1.09 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  9.828 ns |  0.2214 ns | 0.3030 ns |  1.25 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 39.893 ns |  0.2204 ns | 0.1954 ns |  5.06 |    0.08 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 48.171 ns |  0.4772 ns | 0.4230 ns |  6.10 |    0.11 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  7.893 ns |  0.1576 ns | 0.1316 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 10.805 ns |  0.2131 ns | 0.0117 ns |  1.38 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 12.595 ns |  1.1125 ns | 0.0610 ns |  1.61 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.210 ns |  7.0304 ns | 0.3854 ns |  1.17 |    0.04 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  9.074 ns |  8.4304 ns | 0.4621 ns |  1.16 |    0.05 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  9.545 ns |  0.4759 ns | 0.0261 ns |  1.22 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 39.660 ns | 10.5845 ns | 0.5802 ns |  5.06 |    0.07 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 47.364 ns |  9.9740 ns | 0.5467 ns |  6.04 |    0.07 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  7.842 ns |  0.9321 ns | 0.0511 ns |  1.00 |    0.01 |         - |          NA |
