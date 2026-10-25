```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.705 ns | 0.0279 ns | 0.0233 ns |  1.47 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.767 ns | 0.0138 ns | 0.0122 ns |  1.60 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.790 ns | 0.0037 ns | 0.0033 ns |  1.13 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.327 ns | 0.0082 ns | 0.0073 ns |  1.20 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.735 ns | 0.0190 ns | 0.0168 ns |  1.36 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.758 ns | 0.0197 ns | 0.0175 ns |  5.42 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.834 ns | 0.0774 ns | 0.0605 ns |  6.59 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.629 ns | 0.0059 ns | 0.0049 ns |  1.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.443 ns | 0.0439 ns | 0.0024 ns |  1.43 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.783 ns | 0.2286 ns | 0.0125 ns |  1.59 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.795 ns | 0.0655 ns | 0.0036 ns |  1.13 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.359 ns | 0.2647 ns | 0.0145 ns |  1.19 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.908 ns | 0.2701 ns | 0.0148 ns |  1.37 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.847 ns | 1.1808 ns | 0.0647 ns |  5.39 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.407 ns | 0.1074 ns | 0.0059 ns |  6.60 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.692 ns | 0.0669 ns | 0.0037 ns |  1.00 |         - |          NA |
