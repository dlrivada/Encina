```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.442 ns |  0.0199 ns | 0.0166 ns |  1.44 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.818 ns |  0.0815 ns | 0.0680 ns |  1.60 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.350 ns |  0.0058 ns | 0.0051 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.735 ns |  0.0191 ns | 0.0160 ns |  1.36 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.842 ns |  0.0672 ns | 0.0524 ns |  5.41 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.879 ns |  0.1045 ns | 0.0926 ns |  6.57 |    0.02 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.656 ns |  0.0356 ns | 0.0298 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.816 ns |  0.0500 ns | 0.0391 ns |  1.13 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.482 ns |  0.3497 ns | 0.0192 ns |  1.43 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.825 ns |  1.2152 ns | 0.0666 ns |  1.59 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.359 ns |  0.1785 ns | 0.0098 ns |  1.19 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.990 ns |  1.0332 ns | 0.0566 ns |  1.37 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.805 ns |  0.2165 ns | 0.0119 ns |  5.37 |    0.10 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 58.341 ns | 29.8636 ns | 1.6369 ns |  6.69 |    0.21 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.723 ns |  3.5314 ns | 0.1936 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.810 ns |  0.0964 ns | 0.0053 ns |  1.12 |    0.02 |         - |          NA |
