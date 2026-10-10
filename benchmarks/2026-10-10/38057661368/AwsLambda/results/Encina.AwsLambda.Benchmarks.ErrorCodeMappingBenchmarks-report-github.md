```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 15.05 ns |  0.111 ns | 0.104 ns |  1.47 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 14.78 ns |  0.040 ns | 0.036 ns |  1.44 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     | 11.40 ns |  0.113 ns | 0.101 ns |  1.11 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 12.78 ns |  0.094 ns | 0.084 ns |  1.24 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 13.39 ns |  0.165 ns | 0.155 ns |  1.30 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 55.55 ns |  0.188 ns | 0.175 ns |  5.41 |    0.12 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 67.43 ns |  0.554 ns | 0.519 ns |  6.57 |    0.15 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     | 10.27 ns |  0.245 ns | 0.229 ns |  1.00 |    0.03 |         - |          NA |
|                                 |            |                |             |             |          |           |          |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 15.03 ns |  4.130 ns | 0.226 ns |  1.43 |    0.06 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 15.84 ns |  1.237 ns | 0.068 ns |  1.50 |    0.06 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           | 11.74 ns |  1.176 ns | 0.064 ns |  1.11 |    0.04 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 13.90 ns |  1.065 ns | 0.058 ns |  1.32 |    0.05 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 13.59 ns |  2.241 ns | 0.123 ns |  1.29 |    0.05 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 55.55 ns |  4.061 ns | 0.223 ns |  5.27 |    0.21 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 67.85 ns | 13.087 ns | 0.717 ns |  6.44 |    0.26 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           | 10.56 ns |  8.531 ns | 0.468 ns |  1.00 |    0.05 |         - |          NA |
