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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.448 ns | 0.0370 ns | 0.0328 ns |  1.44 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.764 ns | 0.0182 ns | 0.0142 ns |  1.59 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.340 ns | 0.0111 ns | 0.0098 ns |  1.20 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.845 ns | 0.0222 ns | 0.0185 ns |  1.37 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.818 ns | 0.0746 ns | 0.0661 ns |  5.42 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.816 ns | 0.0532 ns | 0.0444 ns |  6.58 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.638 ns | 0.0148 ns | 0.0116 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.790 ns | 0.0057 ns | 0.0044 ns |  1.13 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.439 ns | 0.0664 ns | 0.0036 ns |  1.44 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.688 ns | 0.2197 ns | 0.0120 ns |  1.59 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.341 ns | 0.0445 ns | 0.0024 ns |  1.20 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 12.808 ns | 0.6102 ns | 0.0334 ns |  1.48 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.822 ns | 1.0152 ns | 0.0556 ns |  5.43 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.331 ns | 0.5386 ns | 0.0295 ns |  6.65 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.626 ns | 0.1328 ns | 0.0073 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.791 ns | 0.0519 ns | 0.0028 ns |  1.13 |         - |          NA |
