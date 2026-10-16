```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 16.46 ns |  0.062 ns | 0.055 ns |  1.38 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 18.45 ns |  0.049 ns | 0.045 ns |  1.55 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 14.12 ns |  0.025 ns | 0.022 ns |  1.18 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 15.13 ns |  0.018 ns | 0.016 ns |  1.27 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 65.26 ns |  0.512 ns | 0.454 ns |  5.47 |    0.04 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 78.53 ns |  0.465 ns | 0.435 ns |  6.59 |    0.04 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 11.92 ns |  0.044 ns | 0.039 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 12.96 ns |  0.161 ns | 0.151 ns |  1.09 |    0.01 |         - |          NA |
|                                 |            |                |             |             |          |           |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 16.59 ns |  1.249 ns | 0.068 ns |  1.37 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 18.39 ns |  1.130 ns | 0.062 ns |  1.52 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 14.27 ns |  5.162 ns | 0.283 ns |  1.18 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 15.27 ns |  1.205 ns | 0.066 ns |  1.26 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 67.77 ns |  6.316 ns | 0.346 ns |  5.60 |    0.03 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 80.01 ns | 14.098 ns | 0.773 ns |  6.61 |    0.06 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 12.11 ns |  0.988 ns | 0.054 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 13.04 ns |  0.710 ns | 0.039 ns |  1.08 |    0.01 |         - |          NA |
