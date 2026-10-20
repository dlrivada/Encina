```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.16 ns | 0.036 ns | 0.028 ns |  1.37 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.18 ns | 0.016 ns | 0.014 ns |  1.54 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.36 ns | 0.014 ns | 0.012 ns |  1.22 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.93 ns | 0.016 ns | 0.013 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.45 ns | 0.054 ns | 0.045 ns |  5.21 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 75.84 ns | 0.058 ns | 0.051 ns |  6.43 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.79 ns | 0.011 ns | 0.009 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.26 ns | 0.184 ns | 0.172 ns |  1.12 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.94 ns | 0.516 ns | 0.028 ns |  1.38 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.95 ns | 0.382 ns | 0.021 ns |  1.55 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.05 ns | 0.164 ns | 0.009 ns |  1.21 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.62 ns | 0.202 ns | 0.011 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.52 ns | 0.837 ns | 0.046 ns |  5.23 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 76.14 ns | 0.732 ns | 0.040 ns |  6.58 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.57 ns | 0.159 ns | 0.009 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.33 ns | 0.156 ns | 0.009 ns |  1.15 |         - |          NA |
