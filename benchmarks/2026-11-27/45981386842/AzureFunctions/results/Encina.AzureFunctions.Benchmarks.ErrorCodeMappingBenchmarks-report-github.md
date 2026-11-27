```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 11.365 ns |  0.0401 ns | 0.0356 ns |  1.38 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.654 ns |  0.1121 ns | 0.1049 ns |  1.54 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  9.014 ns |  0.0162 ns | 0.0135 ns |  1.10 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  9.958 ns |  0.0238 ns | 0.0211 ns |  1.21 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 41.256 ns |  0.1002 ns | 0.0937 ns |  5.01 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 50.053 ns |  0.1769 ns | 0.1568 ns |  6.08 |    0.03 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.227 ns |  0.0352 ns | 0.0329 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.818 ns |  0.0161 ns | 0.0143 ns |  1.07 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 11.371 ns |  0.4797 ns | 0.0263 ns |  1.39 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.047 ns |  0.1454 ns | 0.0080 ns |  1.59 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  9.272 ns |  0.1345 ns | 0.0074 ns |  1.13 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 10.068 ns |  0.1995 ns | 0.0109 ns |  1.23 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 41.187 ns |  1.4622 ns | 0.0801 ns |  5.03 |    0.01 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 49.575 ns | 12.0004 ns | 0.6578 ns |  6.05 |    0.07 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.195 ns |  0.1045 ns | 0.0057 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.076 ns |  0.1206 ns | 0.0066 ns |  1.11 |    0.00 |         - |          NA |
