```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.434 ns |  0.2060 ns | 0.2955 ns |  1.55 |    0.06 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.809 ns |  0.2314 ns | 0.3244 ns |  1.77 |    0.06 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.750 ns |  0.1395 ns | 0.1165 ns |  1.11 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.699 ns |  0.1145 ns | 0.0894 ns |  1.26 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 34.499 ns |  0.3705 ns | 0.3466 ns |  5.65 |    0.13 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 39.793 ns |  0.5785 ns | 0.5411 ns |  6.52 |    0.16 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  6.104 ns |  0.1408 ns | 0.1317 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.463 ns |  0.1165 ns | 0.1090 ns |  1.06 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.539 ns |  5.6100 ns | 0.3075 ns |  1.55 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.886 ns |  2.1948 ns | 0.1203 ns |  1.77 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.851 ns |  2.8045 ns | 0.1537 ns |  1.12 |    0.03 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.923 ns |  0.6127 ns | 0.0336 ns |  1.29 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 34.661 ns |  4.4033 ns | 0.2414 ns |  5.64 |    0.10 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 39.405 ns | 11.4467 ns | 0.6274 ns |  6.42 |    0.13 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.144 ns |  2.0509 ns | 0.1124 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.424 ns |  1.3554 ns | 0.0743 ns |  1.05 |    0.02 |         - |          NA |
