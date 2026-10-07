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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.444 ns | 0.0182 ns | 0.0152 ns |  1.45 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.791 ns | 0.0288 ns | 0.0225 ns |  1.60 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.348 ns | 0.0124 ns | 0.0104 ns |  1.20 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.718 ns | 0.0070 ns | 0.0062 ns |  1.36 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.772 ns | 0.0484 ns | 0.0404 ns |  5.43 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.996 ns | 0.0632 ns | 0.0561 ns |  6.62 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.612 ns | 0.0102 ns | 0.0090 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.815 ns | 0.0104 ns | 0.0097 ns |  1.14 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.438 ns | 0.2235 ns | 0.0122 ns |  1.45 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 14.730 ns | 0.0655 ns | 0.0036 ns |  1.71 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.372 ns | 0.8618 ns | 0.0472 ns |  1.21 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.851 ns | 0.1699 ns | 0.0093 ns |  1.38 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.736 ns | 0.3192 ns | 0.0175 ns |  5.43 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.284 ns | 0.9554 ns | 0.0524 ns |  6.66 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.606 ns | 0.0228 ns | 0.0012 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.820 ns | 0.8166 ns | 0.0448 ns |  1.14 |         - |          NA |
