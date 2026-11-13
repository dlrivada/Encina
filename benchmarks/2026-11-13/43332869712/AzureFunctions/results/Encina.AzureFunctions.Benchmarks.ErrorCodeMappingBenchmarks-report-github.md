```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 14.94 ns |  0.038 ns | 0.030 ns |  1.40 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 17.18 ns |  0.241 ns | 0.201 ns |  1.61 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.72 ns |  0.019 ns | 0.016 ns |  1.29 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.04 ns |  0.123 ns | 0.115 ns |  1.41 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 56.46 ns |  0.316 ns | 0.295 ns |  5.29 |    0.03 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 71.08 ns |  0.600 ns | 0.562 ns |  6.66 |    0.06 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.68 ns |  0.048 ns | 0.040 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.17 ns |  0.009 ns | 0.008 ns |  1.14 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |           |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.92 ns |  3.313 ns | 0.182 ns |  1.39 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 16.45 ns |  0.325 ns | 0.018 ns |  1.53 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.41 ns |  0.400 ns | 0.022 ns |  1.25 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.80 ns |  2.202 ns | 0.121 ns |  1.38 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.50 ns |  1.886 ns | 0.103 ns |  5.16 |    0.03 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 72.01 ns | 18.824 ns | 1.032 ns |  6.70 |    0.09 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.76 ns |  1.229 ns | 0.067 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.39 ns |  0.090 ns | 0.005 ns |  1.15 |    0.01 |         - |          NA |
