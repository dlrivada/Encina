```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 10.524 ns | 0.2185 ns | 0.1706 ns | 10.478 ns |  1.40 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 11.640 ns | 0.0932 ns | 0.0826 ns | 11.675 ns |  1.55 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  8.385 ns | 0.0103 ns | 0.0092 ns |  8.383 ns |  1.11 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  9.223 ns | 0.0113 ns | 0.0105 ns |  9.223 ns |  1.23 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 38.180 ns | 0.0651 ns | 0.0577 ns | 38.176 ns |  5.07 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 46.814 ns | 0.0597 ns | 0.0499 ns | 46.799 ns |  6.22 |    0.02 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  7.529 ns | 0.0272 ns | 0.0241 ns |  7.525 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.143 ns | 0.0097 ns | 0.0091 ns |  8.144 ns |  1.08 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 10.540 ns | 0.0106 ns | 0.0145 ns | 10.538 ns |  1.35 |    0.08 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 11.769 ns | 0.1611 ns | 0.2205 ns | 11.726 ns |  1.51 |    0.10 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          |  8.587 ns | 0.0044 ns | 0.0060 ns |  8.586 ns |  1.10 |    0.07 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          |  9.335 ns | 0.0092 ns | 0.0135 ns |  9.340 ns |  1.20 |    0.07 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 38.192 ns | 0.0427 ns | 0.0626 ns | 38.197 ns |  4.89 |    0.30 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 46.682 ns | 0.1214 ns | 0.1779 ns | 46.773 ns |  5.98 |    0.36 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          |  7.834 ns | 0.3600 ns | 0.5388 ns |  7.597 ns |  1.00 |    0.09 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          |  8.572 ns | 0.3672 ns | 0.5267 ns |  8.354 ns |  1.10 |    0.09 |         - |          NA |
