```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.439 ns | 0.0229 ns | 0.0203 ns |  1.44 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.772 ns | 0.0240 ns | 0.0213 ns |  1.60 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.791 ns | 0.0052 ns | 0.0040 ns |  1.14 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.401 ns | 0.1049 ns | 0.0930 ns |  1.21 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.765 ns | 0.0283 ns | 0.0236 ns |  1.37 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.753 ns | 0.0196 ns | 0.0164 ns |  5.43 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.878 ns | 0.0213 ns | 0.0166 ns |  6.60 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.615 ns | 0.0072 ns | 0.0063 ns |  1.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.583 ns | 0.3493 ns | 0.0191 ns |  1.30 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.687 ns | 0.1420 ns | 0.0078 ns |  1.41 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.790 ns | 0.0498 ns | 0.0027 ns |  1.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.334 ns | 0.0531 ns | 0.0029 ns |  1.07 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.955 ns | 2.2796 ns | 0.1250 ns |  1.24 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 47.979 ns | 1.1527 ns | 0.0632 ns |  4.96 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.857 ns | 1.0791 ns | 0.0591 ns |  5.98 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  9.679 ns | 0.0995 ns | 0.0055 ns |  1.00 |         - |          NA |
