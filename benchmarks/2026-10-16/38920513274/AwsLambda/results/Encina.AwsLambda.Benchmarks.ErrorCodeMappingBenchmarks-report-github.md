```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.89GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.16 ns | 0.036 ns | 0.030 ns |  1.27 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.16 ns | 0.013 ns | 0.011 ns |  1.42 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.09 ns | 0.004 ns | 0.003 ns |  1.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.35 ns | 0.018 ns | 0.016 ns |  1.12 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.91 ns | 0.016 ns | 0.013 ns |  1.25 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.43 ns | 0.061 ns | 0.048 ns |  4.81 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 75.80 ns | 0.052 ns | 0.043 ns |  5.94 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 12.77 ns | 0.020 ns | 0.018 ns |  1.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.95 ns | 1.264 ns | 0.069 ns |  1.38 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 18.10 ns | 2.703 ns | 0.148 ns |  1.56 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.35 ns | 0.492 ns | 0.027 ns |  1.15 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.04 ns | 0.098 ns | 0.005 ns |  1.21 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.60 ns | 0.271 ns | 0.015 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.49 ns | 0.064 ns | 0.003 ns |  5.22 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 76.08 ns | 0.324 ns | 0.018 ns |  6.56 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.59 ns | 0.544 ns | 0.030 ns |  1.00 |         - |          NA |
