```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 11.328 ns | 0.0500 ns | 0.0468 ns |  1.37 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.837 ns | 0.0651 ns | 0.0609 ns |  1.55 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.823 ns | 0.0166 ns | 0.0155 ns |  1.07 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  9.087 ns | 0.0243 ns | 0.0227 ns |  1.10 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  9.991 ns | 0.0185 ns | 0.0164 ns |  1.21 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 41.413 ns | 0.1238 ns | 0.1097 ns |  5.01 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 50.495 ns | 0.1053 ns | 0.0985 ns |  6.11 |    0.02 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.267 ns | 0.0177 ns | 0.0166 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 11.356 ns | 0.3088 ns | 0.0169 ns |  1.37 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.126 ns | 0.2509 ns | 0.0138 ns |  1.59 |    0.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.117 ns | 0.2282 ns | 0.0125 ns |  1.10 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  9.281 ns | 0.2978 ns | 0.0163 ns |  1.12 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 10.171 ns | 1.3077 ns | 0.0717 ns |  1.23 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 41.192 ns | 1.8217 ns | 0.0999 ns |  4.98 |    0.01 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 49.175 ns | 3.5409 ns | 0.1941 ns |  5.94 |    0.02 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.279 ns | 0.0401 ns | 0.0022 ns |  1.00 |    0.00 |         - |          NA |
