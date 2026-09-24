```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.424 ns | 0.0087 ns | 0.0073 ns |  1.45 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.759 ns | 0.0194 ns | 0.0162 ns |  1.60 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.424 ns | 0.0181 ns | 0.0151 ns |  1.21 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.740 ns | 0.0199 ns | 0.0176 ns |  1.37 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.791 ns | 0.0338 ns | 0.0316 ns |  5.44 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.984 ns | 0.0432 ns | 0.0383 ns |  6.63 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.597 ns | 0.0093 ns | 0.0072 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.799 ns | 0.0071 ns | 0.0063 ns |  1.14 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.734 ns | 0.7496 ns | 0.0411 ns |  1.47 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.700 ns | 0.1229 ns | 0.0067 ns |  1.58 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.367 ns | 0.0760 ns | 0.0042 ns |  1.20 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.866 ns | 0.2333 ns | 0.0128 ns |  1.37 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 48.074 ns | 1.9864 ns | 0.1089 ns |  5.55 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.302 ns | 0.5003 ns | 0.0274 ns |  6.61 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.668 ns | 0.2113 ns | 0.0116 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.802 ns | 0.0606 ns | 0.0033 ns |  1.13 |         - |          NA |
