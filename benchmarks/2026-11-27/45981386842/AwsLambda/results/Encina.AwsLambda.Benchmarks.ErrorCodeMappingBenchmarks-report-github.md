```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.018 ns |  0.1953 ns | 0.2005 ns |  1.40 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  9.776 ns |  0.2228 ns | 0.4552 ns |  1.71 |    0.09 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.133 ns |  0.1062 ns | 0.1418 ns |  1.07 |    0.04 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.827 ns |  0.1616 ns | 0.2873 ns |  1.19 |    0.06 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.769 ns |  0.1784 ns | 0.2054 ns |  1.36 |    0.05 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 33.479 ns |  0.6948 ns | 1.6240 ns |  5.85 |    0.32 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 40.274 ns |  0.8304 ns | 1.8227 ns |  7.03 |    0.36 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.729 ns |  0.1351 ns | 0.1501 ns |  1.00 |    0.04 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.202 ns |  2.3051 ns | 0.1263 ns |  1.62 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  9.957 ns |  9.0990 ns | 0.4987 ns |  1.75 |    0.08 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  7.047 ns | 20.8788 ns | 1.1444 ns |  1.24 |    0.17 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  7.402 ns |  5.6704 ns | 0.3108 ns |  1.30 |    0.05 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.152 ns |  3.6775 ns | 0.2016 ns |  1.43 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 32.184 ns | 20.1914 ns | 1.1068 ns |  5.65 |    0.17 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 38.502 ns |  6.8689 ns | 0.3765 ns |  6.76 |    0.06 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.693 ns |  0.2073 ns | 0.0114 ns |  1.00 |    0.00 |         - |          NA |
