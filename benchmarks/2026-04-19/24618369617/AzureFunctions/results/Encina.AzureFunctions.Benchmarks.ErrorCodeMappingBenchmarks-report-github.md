```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.31 ns | 0.269 ns | 0.252 ns | 16.16 ns |  1.44 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.70 ns | 0.097 ns | 0.081 ns | 18.67 ns |  1.65 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.35 ns | 0.015 ns | 0.013 ns | 13.35 ns |  1.18 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.51 ns | 0.018 ns | 0.017 ns | 15.51 ns |  1.37 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 66.43 ns | 0.286 ns | 0.268 ns | 66.43 ns |  5.88 |    0.04 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 73.62 ns | 0.121 ns | 0.101 ns | 73.60 ns |  6.51 |    0.04 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.31 ns | 0.077 ns | 0.064 ns | 11.27 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.86 ns | 0.009 ns | 0.008 ns | 12.86 ns |  1.14 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 16.32 ns | 0.053 ns | 0.071 ns | 16.30 ns |  1.41 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 18.92 ns | 0.040 ns | 0.056 ns | 18.91 ns |  1.64 |    0.00 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 13.37 ns | 0.015 ns | 0.020 ns | 13.37 ns |  1.16 |    0.00 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 15.68 ns | 0.014 ns | 0.018 ns | 15.69 ns |  1.36 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 64.82 ns | 0.301 ns | 0.441 ns | 64.58 ns |  5.60 |    0.04 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 78.29 ns | 3.206 ns | 4.494 ns | 74.27 ns |  6.77 |    0.38 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.57 ns | 0.005 ns | 0.007 ns | 11.57 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 12.76 ns | 0.017 ns | 0.023 ns | 12.75 ns |  1.10 |    0.00 |         - |          NA |
