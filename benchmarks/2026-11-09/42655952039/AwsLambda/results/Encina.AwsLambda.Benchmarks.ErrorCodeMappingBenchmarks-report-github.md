```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  9.499 ns |  0.1204 ns | 0.1068 ns |  1.64 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.961 ns |  0.1155 ns | 0.1081 ns |  1.90 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  7.203 ns |  0.0463 ns | 0.0411 ns |  1.25 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.974 ns |  0.0819 ns | 0.0640 ns |  1.21 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.865 ns |  0.0555 ns | 0.0463 ns |  1.36 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 35.690 ns |  0.2229 ns | 0.2085 ns |  6.18 |    0.06 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 40.824 ns |  0.3470 ns | 0.3246 ns |  7.07 |    0.08 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.777 ns |  0.0501 ns | 0.0468 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.575 ns |  0.2994 ns | 0.0164 ns |  1.62 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.969 ns |  3.0400 ns | 0.1666 ns |  1.85 |    0.05 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.130 ns |  0.3978 ns | 0.0218 ns |  1.03 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.479 ns |  0.9280 ns | 0.0509 ns |  1.09 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.475 ns |  0.3658 ns | 0.0201 ns |  1.26 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 35.626 ns |  0.8341 ns | 0.0457 ns |  6.01 |    0.12 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 39.929 ns | 26.1510 ns | 1.4334 ns |  6.74 |    0.25 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.929 ns |  2.5676 ns | 0.1407 ns |  1.00 |    0.03 |         - |          NA |
