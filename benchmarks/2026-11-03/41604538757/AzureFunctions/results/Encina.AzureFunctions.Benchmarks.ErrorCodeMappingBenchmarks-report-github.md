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
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.446 ns |  0.0185 ns | 0.0154 ns |  1.43 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.781 ns |  0.0215 ns | 0.0179 ns |  1.58 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.377 ns |  0.0565 ns | 0.0472 ns |  1.19 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.910 ns |  0.0331 ns | 0.0294 ns |  1.37 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.838 ns |  0.0150 ns | 0.0125 ns |  5.37 |    0.06 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 57.165 ns |  0.1240 ns | 0.0968 ns |  6.55 |    0.08 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.725 ns |  0.1188 ns | 0.1053 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.804 ns |  0.0039 ns | 0.0030 ns |  1.12 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.478 ns |  0.8635 ns | 0.0473 ns |  1.45 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.793 ns |  0.1335 ns | 0.0073 ns |  1.60 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.353 ns |  0.0287 ns | 0.0016 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.978 ns |  1.7962 ns | 0.0985 ns |  1.39 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 47.762 ns | 21.4879 ns | 1.1778 ns |  5.54 |    0.12 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.338 ns |  0.4292 ns | 0.0235 ns |  6.65 |    0.01 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.619 ns |  0.1474 ns | 0.0081 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.800 ns |  0.0845 ns | 0.0046 ns |  1.14 |    0.00 |         - |          NA |
