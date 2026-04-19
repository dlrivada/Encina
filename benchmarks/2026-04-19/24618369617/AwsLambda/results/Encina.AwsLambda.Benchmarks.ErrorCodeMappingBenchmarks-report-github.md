```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.17 ns | 0.034 ns | 0.032 ns | 16.16 ns |  1.41 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.14 ns | 0.045 ns | 0.042 ns | 18.13 ns |  1.58 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.80 ns | 0.022 ns | 0.018 ns | 12.79 ns |  1.12 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.34 ns | 0.007 ns | 0.005 ns | 14.35 ns |  1.25 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.60 ns | 0.018 ns | 0.015 ns | 15.60 ns |  1.36 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.27 ns | 0.255 ns | 0.226 ns | 61.17 ns |  5.34 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 75.34 ns | 0.101 ns | 0.090 ns | 75.32 ns |  6.56 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.48 ns | 0.012 ns | 0.009 ns | 11.47 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 15.92 ns | 0.013 ns | 0.019 ns | 15.92 ns |  1.39 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 18.22 ns | 0.009 ns | 0.012 ns | 18.22 ns |  1.59 |    0.00 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 13.04 ns | 0.017 ns | 0.024 ns | 13.03 ns |  1.14 |    0.00 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 14.03 ns | 0.007 ns | 0.010 ns | 14.03 ns |  1.22 |    0.00 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 15.60 ns | 0.012 ns | 0.015 ns | 15.60 ns |  1.36 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 60.46 ns | 0.193 ns | 0.283 ns | 60.31 ns |  5.27 |    0.03 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 75.21 ns | 0.060 ns | 0.084 ns | 75.17 ns |  6.55 |    0.01 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.48 ns | 0.013 ns | 0.019 ns | 11.47 ns |  1.00 |    0.00 |         - |          NA |
