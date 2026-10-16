```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.11 ns | 0.101 ns | 0.095 ns |  1.41 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 17.76 ns | 0.026 ns | 0.022 ns |  1.55 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.71 ns | 0.014 ns | 0.012 ns |  1.11 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.34 ns | 0.013 ns | 0.011 ns |  1.17 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.48 ns | 0.015 ns | 0.012 ns |  1.35 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 60.39 ns | 0.087 ns | 0.073 ns |  5.28 |    0.06 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 73.57 ns | 0.036 ns | 0.034 ns |  6.43 |    0.08 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.45 ns | 0.150 ns | 0.140 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 16.97 ns | 1.745 ns | 0.096 ns |  1.52 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.85 ns | 0.273 ns | 0.015 ns |  1.60 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.73 ns | 1.393 ns | 0.076 ns |  1.14 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.40 ns | 0.172 ns | 0.009 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.73 ns | 0.194 ns | 0.011 ns |  1.41 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.33 ns | 0.870 ns | 0.048 ns |  5.40 |    0.01 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 73.94 ns | 0.370 ns | 0.020 ns |  6.62 |    0.01 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.16 ns | 0.209 ns | 0.011 ns |  1.00 |    0.00 |         - |          NA |
