```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.485 ns | 0.0595 ns | 0.0557 ns |  1.45 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.845 ns | 0.1264 ns | 0.1182 ns |  1.60 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.806 ns | 0.0081 ns | 0.0076 ns |  1.14 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.379 ns | 0.0659 ns | 0.0617 ns |  1.20 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.773 ns | 0.0568 ns | 0.0531 ns |  1.36 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.909 ns | 0.1905 ns | 0.1689 ns |  5.43 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.884 ns | 0.0526 ns | 0.0467 ns |  6.59 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.636 ns | 0.0155 ns | 0.0138 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.438 ns | 0.0976 ns | 0.0053 ns |  1.43 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.690 ns | 0.0786 ns | 0.0043 ns |  1.58 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.999 ns | 4.0966 ns | 0.2246 ns |  1.15 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.397 ns | 1.6416 ns | 0.0900 ns |  1.20 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.871 ns | 0.1454 ns | 0.0080 ns |  1.37 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.922 ns | 3.4689 ns | 0.1901 ns |  5.41 |    0.05 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.451 ns | 1.7906 ns | 0.0981 ns |  6.62 |    0.06 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.678 ns | 1.7369 ns | 0.0952 ns |  1.00 |    0.01 |         - |          NA |
