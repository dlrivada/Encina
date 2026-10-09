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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  7.912 ns | 0.0489 ns | 0.0434 ns |  1.44 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  8.965 ns | 0.1664 ns | 0.1557 ns |  1.63 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  5.758 ns | 0.0425 ns | 0.0376 ns |  1.04 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.171 ns | 0.0686 ns | 0.0608 ns |  1.12 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  8.504 ns | 0.0376 ns | 0.0333 ns |  1.54 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 29.029 ns | 0.1927 ns | 0.1803 ns |  5.27 |    0.09 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 34.886 ns | 0.3405 ns | 0.3018 ns |  6.33 |    0.12 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.513 ns | 0.1118 ns | 0.0991 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  7.901 ns | 0.2327 ns | 0.0128 ns |  1.46 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  9.348 ns | 9.0175 ns | 0.4943 ns |  1.73 |    0.08 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  5.798 ns | 0.2383 ns | 0.0131 ns |  1.07 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.243 ns | 2.4171 ns | 0.1325 ns |  1.15 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.544 ns | 2.8326 ns | 0.1553 ns |  1.40 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 28.989 ns | 1.8553 ns | 0.1017 ns |  5.36 |    0.04 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 44.573 ns | 7.6532 ns | 0.4195 ns |  8.25 |    0.09 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.406 ns | 0.8749 ns | 0.0480 ns |  1.00 |    0.01 |         - |          NA |
