```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.450 ns | 0.0201 ns | 0.0178 ns |  1.44 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.808 ns | 0.0661 ns | 0.0586 ns |  1.60 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.348 ns | 0.0156 ns | 0.0130 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.725 ns | 0.0121 ns | 0.0101 ns |  1.36 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.810 ns | 0.0613 ns | 0.0543 ns |  5.43 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 57.021 ns | 0.0552 ns | 0.0431 ns |  6.61 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.621 ns | 0.0146 ns | 0.0122 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.872 ns | 0.0948 ns | 0.0840 ns |  1.15 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.451 ns | 0.3945 ns | 0.0216 ns |  1.43 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.714 ns | 0.0581 ns | 0.0032 ns |  1.58 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.342 ns | 0.5294 ns | 0.0290 ns |  1.19 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 12.014 ns | 3.4884 ns | 0.1912 ns |  1.38 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.887 ns | 1.9548 ns | 0.1072 ns |  5.39 |    0.08 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 58.062 ns | 8.1935 ns | 0.4491 ns |  6.68 |    0.11 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.699 ns | 2.7850 ns | 0.1527 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.783 ns | 0.0566 ns | 0.0031 ns |  1.12 |    0.02 |         - |          NA |
