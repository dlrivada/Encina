```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.33GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.880 ns | 0.1919 ns | 0.2053 ns |  1.51 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.253 ns | 0.1850 ns | 0.1640 ns |  1.74 |    0.05 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.248 ns | 0.0917 ns | 0.0813 ns |  1.06 |    0.03 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.419 ns | 0.1096 ns | 0.0971 ns |  1.09 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.269 ns | 0.0600 ns | 0.0501 ns |  1.23 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 32.982 ns | 0.1120 ns | 0.0874 ns |  5.60 |    0.12 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 37.583 ns | 0.1571 ns | 0.1470 ns |  6.38 |    0.14 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.890 ns | 0.1344 ns | 0.1320 ns |  1.00 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.863 ns | 1.7493 ns | 0.0959 ns |  1.52 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.095 ns | 0.7680 ns | 0.0421 ns |  1.74 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.284 ns | 2.7244 ns | 0.1493 ns |  1.08 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.543 ns | 5.4830 ns | 0.3005 ns |  1.13 |    0.05 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.479 ns | 0.1923 ns | 0.0105 ns |  1.29 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 32.990 ns | 8.7991 ns | 0.4823 ns |  5.68 |    0.08 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 37.836 ns | 5.1739 ns | 0.2836 ns |  6.51 |    0.05 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.813 ns | 0.6399 ns | 0.0351 ns |  1.00 |    0.01 |         - |          NA |
