```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.06 ns | 0.067 ns | 0.056 ns |  1.42 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 17.77 ns | 0.009 ns | 0.007 ns |  1.57 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.67 ns | 0.012 ns | 0.011 ns |  1.12 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.34 ns | 0.017 ns | 0.013 ns |  1.18 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.63 ns | 0.024 ns | 0.022 ns |  1.39 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 60.33 ns | 0.064 ns | 0.050 ns |  5.35 |    0.06 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 73.58 ns | 0.154 ns | 0.129 ns |  6.52 |    0.08 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.29 ns | 0.143 ns | 0.134 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 16.03 ns | 0.241 ns | 0.013 ns |  1.44 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.65 ns | 0.444 ns | 0.024 ns |  1.59 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.67 ns | 0.048 ns | 0.003 ns |  1.14 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.35 ns | 0.290 ns | 0.016 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.86 ns | 2.821 ns | 0.155 ns |  1.43 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.29 ns | 0.649 ns | 0.036 ns |  5.42 |    0.01 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 74.18 ns | 2.962 ns | 0.162 ns |  6.67 |    0.02 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.13 ns | 0.304 ns | 0.017 ns |  1.00 |    0.00 |         - |          NA |
