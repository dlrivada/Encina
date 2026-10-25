```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.426 ns | 0.0148 ns | 0.0123 ns | 12.423 ns |  1.44 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.765 ns | 0.0092 ns | 0.0082 ns | 13.764 ns |  1.60 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.799 ns | 0.0084 ns | 0.0074 ns |  9.797 ns |  1.14 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.350 ns | 0.0182 ns | 0.0161 ns | 10.343 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.720 ns | 0.0067 ns | 0.0059 ns | 11.719 ns |  1.36 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.802 ns | 0.0287 ns | 0.0224 ns | 46.798 ns |  5.43 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.804 ns | 0.0199 ns | 0.0156 ns | 56.805 ns |  6.59 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.618 ns | 0.0107 ns | 0.0100 ns |  8.614 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 12.515 ns | 0.0542 ns | 0.0760 ns | 12.566 ns |  1.45 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 13.779 ns | 0.0078 ns | 0.0114 ns | 13.776 ns |  1.59 |    0.01 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          |  9.797 ns | 0.0031 ns | 0.0042 ns |  9.796 ns |  1.13 |    0.00 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          | 16.031 ns | 3.9819 ns | 5.5820 ns | 20.899 ns |  1.85 |    0.63 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 11.866 ns | 0.0119 ns | 0.0167 ns | 11.862 ns |  1.37 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 46.797 ns | 0.0232 ns | 0.0340 ns | 46.790 ns |  5.41 |    0.02 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 57.586 ns | 0.2497 ns | 0.3501 ns | 57.336 ns |  6.65 |    0.05 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          |  8.656 ns | 0.0214 ns | 0.0314 ns |  8.651 ns |  1.00 |    0.01 |         - |          NA |
