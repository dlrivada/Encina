```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  7.997 ns |  0.0640 ns | 0.0534 ns |  1.45 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  9.054 ns |  0.1936 ns | 0.1811 ns |  1.64 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  5.917 ns |  0.1230 ns | 0.1150 ns |  1.07 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.272 ns |  0.1086 ns | 0.0963 ns |  1.13 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.371 ns |  0.1494 ns | 0.1324 ns |  1.33 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 29.007 ns |  0.2055 ns | 0.1822 ns |  5.25 |    0.10 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 35.247 ns |  0.5250 ns | 0.4654 ns |  6.38 |    0.14 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.529 ns |  0.1053 ns | 0.0985 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.107 ns |  3.5011 ns | 0.1919 ns |  1.50 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  8.832 ns |  1.1666 ns | 0.0639 ns |  1.63 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  5.838 ns |  0.6792 ns | 0.0372 ns |  1.08 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.396 ns |  2.2508 ns | 0.1234 ns |  1.18 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.611 ns |  3.0274 ns | 0.1659 ns |  1.41 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 30.073 ns | 13.0772 ns | 0.7168 ns |  5.55 |    0.12 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 37.137 ns | 20.9506 ns | 1.1484 ns |  6.86 |    0.19 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.415 ns |  0.5286 ns | 0.0290 ns |  1.00 |    0.01 |         - |          NA |
