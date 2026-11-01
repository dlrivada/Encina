```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.18 ns | 0.046 ns | 0.041 ns |  1.37 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.18 ns | 0.012 ns | 0.009 ns |  1.54 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.37 ns | 0.010 ns | 0.009 ns |  1.22 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.92 ns | 0.007 ns | 0.007 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.45 ns | 0.048 ns | 0.042 ns |  5.22 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 75.84 ns | 0.037 ns | 0.031 ns |  6.44 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.78 ns | 0.010 ns | 0.009 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 14.66 ns | 0.046 ns | 0.041 ns |  1.24 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 15.94 ns | 0.009 ns | 0.014 ns |  1.38 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 17.96 ns | 0.017 ns | 0.022 ns |  1.56 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 14.05 ns | 0.009 ns | 0.013 ns |  1.22 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 15.62 ns | 0.013 ns | 0.019 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 60.59 ns | 0.055 ns | 0.075 ns |  5.25 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 76.16 ns | 0.024 ns | 0.034 ns |  6.59 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.55 ns | 0.005 ns | 0.006 ns |  1.00 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 13.34 ns | 0.007 ns | 0.010 ns |  1.16 |         - |          NA |
