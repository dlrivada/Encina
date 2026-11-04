```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 13.736 ns |  0.0204 ns | 0.0159 ns |  1.59 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.771 ns |  0.0114 ns | 0.0101 ns |  1.59 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.360 ns |  0.0052 ns | 0.0048 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.737 ns |  0.0048 ns | 0.0042 ns |  1.36 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.830 ns |  0.0357 ns | 0.0316 ns |  5.41 |    0.00 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.859 ns |  0.0262 ns | 0.0205 ns |  6.57 |    0.00 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.653 ns |  0.0033 ns | 0.0026 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.802 ns |  0.0081 ns | 0.0068 ns |  1.13 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 14.063 ns |  0.3272 ns | 0.0179 ns |  1.55 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.839 ns |  1.5356 ns | 0.0842 ns |  1.52 |    0.01 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.380 ns |  1.2187 ns | 0.0668 ns |  1.14 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.864 ns |  0.0709 ns | 0.0039 ns |  1.30 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 49.104 ns | 27.1300 ns | 1.4871 ns |  5.40 |    0.14 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.356 ns |  0.6999 ns | 0.0384 ns |  6.31 |    0.03 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  9.091 ns |  0.9751 ns | 0.0534 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.797 ns |  0.0783 ns | 0.0043 ns |  1.08 |    0.01 |         - |          NA |
