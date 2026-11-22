```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.528 ns | 0.0875 ns | 0.0731 ns | 12.533 ns |  1.45 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.766 ns | 0.0136 ns | 0.0121 ns | 13.761 ns |  1.59 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.829 ns | 0.0407 ns | 0.0381 ns |  9.808 ns |  1.14 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.377 ns | 0.0497 ns | 0.0465 ns | 10.351 ns |  1.20 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.741 ns | 0.0060 ns | 0.0053 ns | 11.741 ns |  1.36 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.888 ns | 0.1456 ns | 0.1290 ns | 46.830 ns |  5.42 |    0.03 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.958 ns | 0.1062 ns | 0.0887 ns | 56.941 ns |  6.59 |    0.03 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.650 ns | 0.0385 ns | 0.0342 ns |  8.631 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 12.551 ns | 0.0745 ns | 0.1044 ns | 12.626 ns |  1.45 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 13.725 ns | 0.0466 ns | 0.0638 ns | 13.699 ns |  1.59 |    0.01 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          |  9.855 ns | 0.0380 ns | 0.0545 ns |  9.837 ns |  1.14 |    0.01 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 10.423 ns | 0.0450 ns | 0.0646 ns | 10.427 ns |  1.20 |    0.01 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 11.886 ns | 0.0248 ns | 0.0339 ns | 11.879 ns |  1.37 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 46.895 ns | 0.0956 ns | 0.1308 ns | 46.830 ns |  5.42 |    0.03 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 57.445 ns | 0.1060 ns | 0.1520 ns | 57.371 ns |  6.63 |    0.04 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          |  8.658 ns | 0.0340 ns | 0.0465 ns |  8.644 ns |  1.00 |    0.01 |         - |          NA |
