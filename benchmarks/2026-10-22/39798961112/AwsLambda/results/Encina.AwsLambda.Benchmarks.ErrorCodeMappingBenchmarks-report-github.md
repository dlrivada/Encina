```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.08 ns | 0.034 ns | 0.027 ns |  1.44 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 17.79 ns | 0.015 ns | 0.013 ns |  1.60 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.72 ns | 0.011 ns | 0.010 ns |  1.14 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.36 ns | 0.014 ns | 0.011 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.51 ns | 0.015 ns | 0.013 ns |  1.39 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 60.49 ns | 0.191 ns | 0.160 ns |  5.43 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 73.71 ns | 0.200 ns | 0.167 ns |  6.61 |    0.02 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.15 ns | 0.012 ns | 0.011 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 16.07 ns | 0.024 ns | 0.036 ns |  1.44 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 17.98 ns | 0.096 ns | 0.141 ns |  1.61 |    0.01 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.73 ns | 0.016 ns | 0.022 ns |  1.14 |    0.00 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.37 ns | 0.010 ns | 0.014 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 15.72 ns | 0.013 ns | 0.019 ns |  1.41 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 60.38 ns | 0.043 ns | 0.060 ns |  5.42 |    0.01 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 73.98 ns | 0.057 ns | 0.080 ns |  6.63 |    0.01 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.15 ns | 0.010 ns | 0.014 ns |  1.00 |    0.00 |         - |          NA |
