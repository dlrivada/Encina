```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.15 ns | 0.022 ns | 0.017 ns |  1.37 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.16 ns | 0.017 ns | 0.013 ns |  1.54 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.09 ns | 0.006 ns | 0.005 ns |  1.11 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.35 ns | 0.013 ns | 0.011 ns |  1.22 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.90 ns | 0.035 ns | 0.028 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.39 ns | 0.077 ns | 0.069 ns |  5.22 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 75.71 ns | 0.048 ns | 0.045 ns |  6.43 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.77 ns | 0.011 ns | 0.009 ns |  1.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.94 ns | 0.462 ns | 0.025 ns |  1.38 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.91 ns | 0.135 ns | 0.007 ns |  1.55 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.32 ns | 0.257 ns | 0.014 ns |  1.15 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.39 ns | 0.938 ns | 0.051 ns |  1.24 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.60 ns | 2.409 ns | 0.132 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.46 ns | 0.367 ns | 0.020 ns |  5.23 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 76.08 ns | 0.880 ns | 0.048 ns |  6.58 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.56 ns | 0.236 ns | 0.013 ns |  1.00 |         - |          NA |
