```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.442 ns | 0.0210 ns | 0.0164 ns |  1.44 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.782 ns | 0.0387 ns | 0.0343 ns |  1.60 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.344 ns | 0.0059 ns | 0.0049 ns |  1.20 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.736 ns | 0.0277 ns | 0.0231 ns |  1.36 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.829 ns | 0.1037 ns | 0.0919 ns |  5.42 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.803 ns | 0.0420 ns | 0.0351 ns |  6.58 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.635 ns | 0.0084 ns | 0.0070 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.797 ns | 0.0063 ns | 0.0049 ns |  1.13 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.454 ns | 0.1279 ns | 0.0070 ns |  1.45 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.727 ns | 1.4482 ns | 0.0794 ns |  1.60 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.343 ns | 0.1551 ns | 0.0085 ns |  1.20 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.900 ns | 1.0265 ns | 0.0563 ns |  1.38 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.737 ns | 0.2472 ns | 0.0136 ns |  5.43 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.565 ns | 0.3936 ns | 0.0216 ns |  6.69 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.606 ns | 0.0955 ns | 0.0052 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.793 ns | 0.2050 ns | 0.0112 ns |  1.14 |         - |          NA |
