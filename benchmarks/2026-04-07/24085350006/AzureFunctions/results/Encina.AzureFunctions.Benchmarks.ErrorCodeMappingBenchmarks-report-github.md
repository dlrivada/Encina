```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.85 ns | 0.050 ns | 0.042 ns |  1.42 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.33 ns | 0.016 ns | 0.013 ns |  1.64 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.74 ns | 0.016 ns | 0.013 ns |  1.23 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 14.97 ns | 0.010 ns | 0.009 ns |  1.34 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 61.61 ns | 0.045 ns | 0.039 ns |  5.52 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 70.70 ns | 0.079 ns | 0.066 ns |  6.33 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.16 ns | 0.012 ns | 0.011 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.17 ns | 0.017 ns | 0.013 ns |  1.09 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.93 ns | 3.456 ns | 0.189 ns |  1.39 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 18.34 ns | 0.677 ns | 0.037 ns |  1.60 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.41 ns | 0.127 ns | 0.007 ns |  1.17 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 14.68 ns | 0.253 ns | 0.014 ns |  1.28 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.41 ns | 0.508 ns | 0.028 ns |  5.27 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 70.87 ns | 0.717 ns | 0.039 ns |  6.19 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.45 ns | 0.009 ns | 0.000 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.53 ns | 2.040 ns | 0.112 ns |  1.09 |         - |          NA |
