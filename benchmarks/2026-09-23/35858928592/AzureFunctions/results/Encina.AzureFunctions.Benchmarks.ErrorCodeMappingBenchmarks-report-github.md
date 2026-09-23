```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.07 ns | 0.028 ns | 0.024 ns |  1.45 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 17.84 ns | 0.104 ns | 0.087 ns |  1.60 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.36 ns | 0.011 ns | 0.009 ns |  1.20 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.52 ns | 0.010 ns | 0.009 ns |  1.40 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 60.33 ns | 0.052 ns | 0.044 ns |  5.43 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 73.59 ns | 0.084 ns | 0.074 ns |  6.62 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.12 ns | 0.007 ns | 0.006 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 13.69 ns | 0.007 ns | 0.005 ns |  1.23 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 16.02 ns | 0.286 ns | 0.016 ns |  1.44 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.63 ns | 0.300 ns | 0.016 ns |  1.58 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.59 ns | 0.385 ns | 0.021 ns |  1.31 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.95 ns | 0.232 ns | 0.013 ns |  1.43 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.41 ns | 0.334 ns | 0.018 ns |  5.42 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 73.96 ns | 0.956 ns | 0.052 ns |  6.64 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.14 ns | 0.478 ns | 0.026 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.69 ns | 0.195 ns | 0.011 ns |  1.14 |         - |          NA |
