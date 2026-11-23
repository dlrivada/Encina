```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.395 ns |  0.1546 ns | 0.1446 ns |  1.47 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  9.321 ns |  0.1435 ns | 0.1272 ns |  1.63 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.040 ns |  0.0700 ns | 0.0585 ns |  1.06 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.501 ns |  0.0686 ns | 0.0608 ns |  1.14 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  8.233 ns |  0.1868 ns | 0.1919 ns |  1.44 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 30.316 ns |  0.2843 ns | 0.2521 ns |  5.30 |    0.08 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 36.946 ns |  0.3316 ns | 0.2769 ns |  6.46 |    0.10 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.720 ns |  0.0988 ns | 0.0771 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.436 ns |  2.3828 ns | 0.1306 ns |  1.57 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  9.299 ns |  2.7401 ns | 0.1502 ns |  1.73 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.188 ns |  0.7021 ns | 0.0385 ns |  1.15 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.396 ns |  0.9621 ns | 0.0527 ns |  1.19 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.153 ns |  0.5927 ns | 0.0325 ns |  1.51 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 30.415 ns | 10.4297 ns | 0.5717 ns |  5.65 |    0.09 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 36.409 ns | 10.3624 ns | 0.5680 ns |  6.76 |    0.09 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.386 ns |  0.3351 ns | 0.0184 ns |  1.00 |    0.00 |         - |          NA |
