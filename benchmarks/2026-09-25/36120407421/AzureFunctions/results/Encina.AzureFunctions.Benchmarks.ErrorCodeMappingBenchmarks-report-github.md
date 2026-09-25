```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 10.692 ns | 0.0238 ns | 0.0211 ns |  1.36 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 11.955 ns | 0.1048 ns | 0.0875 ns |  1.52 |    0.06 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  8.639 ns | 0.0886 ns | 0.0740 ns |  1.10 |    0.04 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  9.463 ns | 0.0393 ns | 0.0328 ns |  1.20 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 39.221 ns | 0.1159 ns | 0.0967 ns |  4.99 |    0.19 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 48.719 ns | 0.1554 ns | 0.1297 ns |  6.20 |    0.23 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  7.872 ns | 0.1835 ns | 0.3067 ns |  1.00 |    0.05 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.266 ns | 0.1320 ns | 0.1102 ns |  1.05 |    0.04 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 10.841 ns | 0.2943 ns | 0.0161 ns |  1.36 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 12.143 ns | 0.6147 ns | 0.0337 ns |  1.52 |    0.06 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  9.122 ns | 6.2768 ns | 0.3441 ns |  1.14 |    0.06 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  9.671 ns | 7.2797 ns | 0.3990 ns |  1.21 |    0.06 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 39.137 ns | 1.4465 ns | 0.0793 ns |  4.90 |    0.19 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 47.225 ns | 5.2828 ns | 0.2896 ns |  5.91 |    0.23 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  7.999 ns | 6.4835 ns | 0.3554 ns |  1.00 |    0.06 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  8.869 ns | 9.5143 ns | 0.5215 ns |  1.11 |    0.07 |         - |          NA |
