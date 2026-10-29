```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.981 ns |  0.1365 ns | 0.1139 ns |  1.51 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.377 ns |  0.2142 ns | 0.2003 ns |  1.75 |    0.05 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.524 ns |  0.1032 ns | 0.0915 ns |  1.10 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.344 ns |  0.1047 ns | 0.0929 ns |  1.24 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 33.419 ns |  0.6645 ns | 0.6216 ns |  5.62 |    0.16 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 38.323 ns |  0.4428 ns | 0.3925 ns |  6.44 |    0.15 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.949 ns |  0.1346 ns | 0.1322 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.306 ns |  0.1334 ns | 0.1114 ns |  1.06 |    0.03 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  9.052 ns |  3.9649 ns | 0.2173 ns |  1.56 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.124 ns |  2.4355 ns | 0.1335 ns |  1.74 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.662 ns |  1.8877 ns | 0.1035 ns |  1.15 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.523 ns |  0.4523 ns | 0.0248 ns |  1.29 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 33.528 ns | 16.3681 ns | 0.8972 ns |  5.77 |    0.15 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 39.423 ns | 13.0988 ns | 0.7180 ns |  6.78 |    0.14 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.813 ns |  1.5916 ns | 0.0872 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.320 ns |  1.0718 ns | 0.0587 ns |  1.09 |    0.02 |         - |          NA |
