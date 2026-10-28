```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.15 ns | 0.011 ns | 0.010 ns |  1.37 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.20 ns | 0.015 ns | 0.014 ns |  1.54 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.11 ns | 0.011 ns | 0.009 ns |  1.11 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.36 ns | 0.013 ns | 0.011 ns |  1.22 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.93 ns | 0.020 ns | 0.019 ns |  1.35 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 62.16 ns | 0.078 ns | 0.066 ns |  5.27 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 75.83 ns | 0.049 ns | 0.043 ns |  6.43 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.79 ns | 0.013 ns | 0.011 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.97 ns | 1.381 ns | 0.076 ns |  1.36 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.94 ns | 0.279 ns | 0.015 ns |  1.53 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.34 ns | 0.141 ns | 0.008 ns |  1.13 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.05 ns | 0.423 ns | 0.023 ns |  1.19 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.64 ns | 1.029 ns | 0.056 ns |  1.33 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.57 ns | 0.927 ns | 0.051 ns |  5.15 |    0.06 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 76.10 ns | 0.448 ns | 0.025 ns |  6.47 |    0.08 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.76 ns | 3.007 ns | 0.165 ns |  1.00 |    0.02 |         - |          NA |
