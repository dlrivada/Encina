```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 10.458 ns |  0.0092 ns | 0.0072 ns |  1.36 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 11.786 ns |  0.2563 ns | 0.2397 ns |  1.53 |    0.05 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.116 ns |  0.0111 ns | 0.0092 ns |  1.05 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  8.390 ns |  0.0108 ns | 0.0096 ns |  1.09 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  9.218 ns |  0.0290 ns | 0.0243 ns |  1.20 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 38.477 ns |  0.4855 ns | 0.4054 ns |  5.00 |    0.15 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 46.718 ns |  0.1417 ns | 0.1256 ns |  6.07 |    0.17 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  7.709 ns |  0.1808 ns | 0.2220 ns |  1.00 |    0.04 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 11.220 ns | 12.2864 ns | 0.6735 ns |  1.48 |    0.08 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 11.815 ns |  0.4779 ns | 0.0262 ns |  1.56 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.206 ns | 11.4282 ns | 0.6264 ns |  1.22 |    0.07 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  8.642 ns |  7.7974 ns | 0.4274 ns |  1.14 |    0.05 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  9.608 ns |  7.3705 ns | 0.4040 ns |  1.27 |    0.05 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 38.600 ns | 10.2656 ns | 0.5627 ns |  5.10 |    0.06 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 47.903 ns | 12.0480 ns | 0.6604 ns |  6.33 |    0.08 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  7.571 ns |  0.0906 ns | 0.0050 ns |  1.00 |    0.00 |         - |          NA |
