```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 11.315 ns | 0.0430 ns | 0.0402 ns |  1.38 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.763 ns | 0.0872 ns | 0.0815 ns |  1.55 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.811 ns | 0.0224 ns | 0.0209 ns |  1.07 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  9.110 ns | 0.0182 ns | 0.0161 ns |  1.11 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 10.003 ns | 0.0149 ns | 0.0139 ns |  1.22 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 41.219 ns | 0.0343 ns | 0.0304 ns |  5.02 |    0.01 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 50.480 ns | 0.3905 ns | 0.3653 ns |  6.15 |    0.04 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.211 ns | 0.0113 ns | 0.0095 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 11.396 ns | 0.3166 ns | 0.0174 ns |  1.38 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.127 ns | 1.5400 ns | 0.0844 ns |  1.59 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.072 ns | 0.2895 ns | 0.0159 ns |  1.10 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  9.304 ns | 0.2701 ns | 0.0148 ns |  1.13 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 10.084 ns | 0.4277 ns | 0.0234 ns |  1.22 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 41.218 ns | 0.3391 ns | 0.0186 ns |  5.00 |    0.01 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 49.266 ns | 0.7380 ns | 0.0404 ns |  5.98 |    0.01 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.239 ns | 0.2811 ns | 0.0154 ns |  1.00 |    0.00 |         - |          NA |
