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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.454 ns | 0.0279 ns | 0.0247 ns |  1.45 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.761 ns | 0.0134 ns | 0.0126 ns |  1.60 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.342 ns | 0.0099 ns | 0.0088 ns |  1.20 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.715 ns | 0.0115 ns | 0.0096 ns |  1.36 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.792 ns | 0.0363 ns | 0.0340 ns |  5.43 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.929 ns | 0.0937 ns | 0.0732 ns |  6.61 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.610 ns | 0.0042 ns | 0.0037 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.796 ns | 0.0051 ns | 0.0043 ns |  1.14 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.447 ns | 0.1293 ns | 0.0071 ns |  1.36 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.687 ns | 0.0167 ns | 0.0009 ns |  1.50 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.344 ns | 0.0489 ns | 0.0027 ns |  1.13 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.855 ns | 0.2323 ns | 0.0127 ns |  1.30 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 48.408 ns | 0.3021 ns | 0.0166 ns |  5.29 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.319 ns | 1.0477 ns | 0.0574 ns |  6.27 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  9.147 ns | 0.0150 ns | 0.0008 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.790 ns | 0.0408 ns | 0.0022 ns |  1.07 |         - |          NA |
