```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.512 ns | 0.0632 ns | 0.0591 ns |  1.45 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.756 ns | 0.0117 ns | 0.0109 ns |  1.60 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.350 ns | 0.0088 ns | 0.0069 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.789 ns | 0.0451 ns | 0.0421 ns |  1.37 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.937 ns | 0.1878 ns | 0.1756 ns |  5.45 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 57.113 ns | 0.2028 ns | 0.1897 ns |  6.63 |    0.02 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.616 ns | 0.0207 ns | 0.0161 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.857 ns | 0.0565 ns | 0.0501 ns |  1.14 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.452 ns | 0.8987 ns | 0.0493 ns |  1.43 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.700 ns | 0.0714 ns | 0.0039 ns |  1.58 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.350 ns | 0.1193 ns | 0.0065 ns |  1.19 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.945 ns | 2.2829 ns | 0.1251 ns |  1.37 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.802 ns | 1.3542 ns | 0.0742 ns |  5.38 |    0.06 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.437 ns | 3.9779 ns | 0.2180 ns |  6.60 |    0.07 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.697 ns | 1.8933 ns | 0.1038 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.823 ns | 0.9444 ns | 0.0518 ns |  1.13 |    0.01 |         - |          NA |
