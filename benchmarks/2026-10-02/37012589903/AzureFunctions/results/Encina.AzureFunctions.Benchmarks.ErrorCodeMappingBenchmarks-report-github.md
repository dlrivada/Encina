```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.11 ns | 0.020 ns | 0.015 ns |  1.45 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 17.75 ns | 0.016 ns | 0.015 ns |  1.60 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.85 ns | 0.020 ns | 0.018 ns |  1.34 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.57 ns | 0.020 ns | 0.017 ns |  1.40 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 60.34 ns | 0.046 ns | 0.041 ns |  5.43 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 73.57 ns | 0.035 ns | 0.031 ns |  6.62 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.12 ns | 0.010 ns | 0.009 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.71 ns | 0.018 ns | 0.015 ns |  1.14 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 16.10 ns | 1.627 ns | 0.089 ns |  1.45 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.65 ns | 0.379 ns | 0.021 ns |  1.59 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.34 ns | 0.064 ns | 0.003 ns |  1.20 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.70 ns | 0.164 ns | 0.009 ns |  1.41 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.31 ns | 0.599 ns | 0.033 ns |  5.43 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 75.19 ns | 0.396 ns | 0.022 ns |  6.77 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.11 ns | 0.064 ns | 0.003 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.93 ns | 0.238 ns | 0.013 ns |  1.16 |         - |          NA |
