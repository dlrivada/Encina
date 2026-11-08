```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.17 ns | 0.025 ns | 0.020 ns |  1.37 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.19 ns | 0.043 ns | 0.036 ns |  1.55 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.09 ns | 0.007 ns | 0.006 ns |  1.11 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.39 ns | 0.066 ns | 0.058 ns |  1.22 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.91 ns | 0.020 ns | 0.018 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.33 ns | 0.031 ns | 0.029 ns |  5.21 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 75.78 ns | 0.061 ns | 0.051 ns |  6.44 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.77 ns | 0.006 ns | 0.006 ns |  1.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 15.93 ns | 0.011 ns | 0.015 ns |  1.38 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 17.91 ns | 0.023 ns | 0.032 ns |  1.55 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          | 13.34 ns | 0.014 ns | 0.019 ns |  1.16 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 14.03 ns | 0.009 ns | 0.013 ns |  1.22 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 15.60 ns | 0.032 ns | 0.045 ns |  1.35 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 60.52 ns | 0.046 ns | 0.062 ns |  5.24 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 76.07 ns | 0.045 ns | 0.064 ns |  6.59 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          | 11.55 ns | 0.011 ns | 0.015 ns |  1.00 |         - |          NA |
