```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.04 ns | 0.025 ns | 0.024 ns |  1.44 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 17.83 ns | 0.016 ns | 0.014 ns |  1.60 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 13.35 ns | 0.023 ns | 0.019 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.50 ns | 0.020 ns | 0.017 ns |  1.39 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 60.35 ns | 0.054 ns | 0.048 ns |  5.42 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 73.67 ns | 0.038 ns | 0.035 ns |  6.62 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.14 ns | 0.013 ns | 0.010 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.70 ns | 0.021 ns | 0.017 ns |  1.14 |    0.00 |         - |          NA |
|                                 |            |                |             |             |          |          |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 16.04 ns | 0.425 ns | 0.023 ns |  1.44 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 17.64 ns | 0.366 ns | 0.020 ns |  1.58 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.36 ns | 0.221 ns | 0.012 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.71 ns | 0.126 ns | 0.007 ns |  1.41 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 60.43 ns | 4.333 ns | 0.237 ns |  5.43 |    0.03 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 73.94 ns | 1.144 ns | 0.063 ns |  6.64 |    0.03 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 11.14 ns | 0.935 ns | 0.051 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 12.67 ns | 0.172 ns | 0.009 ns |  1.14 |    0.00 |         - |          NA |
