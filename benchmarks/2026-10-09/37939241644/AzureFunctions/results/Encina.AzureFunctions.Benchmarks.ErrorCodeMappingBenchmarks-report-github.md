```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.294 ns | 0.1193 ns | 0.0997 ns |  1.44 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  9.357 ns | 0.0620 ns | 0.0517 ns |  1.62 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.493 ns | 0.0944 ns | 0.0788 ns |  1.12 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.680 ns | 0.0897 ns | 0.0701 ns |  1.33 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 29.867 ns | 0.3478 ns | 0.2904 ns |  5.17 |    0.10 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 36.735 ns | 0.4004 ns | 0.3549 ns |  6.36 |    0.13 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.781 ns | 0.1131 ns | 0.1058 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.126 ns | 0.0609 ns | 0.0509 ns |  1.06 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.327 ns | 2.9586 ns | 0.1622 ns |  1.43 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  9.314 ns | 0.5610 ns | 0.0308 ns |  1.60 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.721 ns | 5.2863 ns | 0.2898 ns |  1.16 |    0.04 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.063 ns | 7.9977 ns | 0.4384 ns |  1.39 |    0.07 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 31.023 ns | 6.0752 ns | 0.3330 ns |  5.33 |    0.05 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 37.436 ns | 4.7676 ns | 0.2613 ns |  6.44 |    0.04 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.816 ns | 0.3601 ns | 0.0197 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.336 ns | 2.6032 ns | 0.1427 ns |  1.09 |    0.02 |         - |          NA |
