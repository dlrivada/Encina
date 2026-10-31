```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.17 ns | 0.023 ns | 0.021 ns |  1.37 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.19 ns | 0.013 ns | 0.012 ns |  1.54 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.35 ns | 0.014 ns | 0.012 ns |  1.22 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.94 ns | 0.013 ns | 0.011 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.51 ns | 0.073 ns | 0.061 ns |  5.21 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 75.86 ns | 0.080 ns | 0.071 ns |  6.43 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.80 ns | 0.009 ns | 0.007 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.14 ns | 0.034 ns | 0.028 ns |  1.11 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.94 ns | 0.091 ns | 0.005 ns |  1.38 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.98 ns | 0.087 ns | 0.005 ns |  1.56 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.07 ns | 0.302 ns | 0.017 ns |  1.22 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.65 ns | 1.463 ns | 0.080 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.53 ns | 0.120 ns | 0.007 ns |  5.24 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 76.18 ns | 0.701 ns | 0.038 ns |  6.59 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.56 ns | 0.049 ns | 0.003 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.36 ns | 0.039 ns | 0.002 ns |  1.16 |         - |          NA |
