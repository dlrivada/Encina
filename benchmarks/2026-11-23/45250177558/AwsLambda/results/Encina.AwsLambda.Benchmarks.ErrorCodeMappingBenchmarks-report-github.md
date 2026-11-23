```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.372 ns |  0.0484 ns | 0.0404 ns |  1.53 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  9.342 ns |  0.0940 ns | 0.0879 ns |  1.71 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.071 ns |  0.0990 ns | 0.0878 ns |  1.11 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.368 ns |  0.1040 ns | 0.0812 ns |  1.16 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.454 ns |  0.0711 ns | 0.0665 ns |  1.36 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 30.230 ns |  0.4394 ns | 0.4110 ns |  5.52 |    0.10 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 36.536 ns |  0.6658 ns | 0.6228 ns |  6.67 |    0.14 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.477 ns |  0.0834 ns | 0.0696 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.307 ns |  0.8172 ns | 0.0448 ns |  1.50 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  9.155 ns |  2.4967 ns | 0.1369 ns |  1.66 |    0.04 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  7.154 ns |  2.5246 ns | 0.1384 ns |  1.30 |    0.03 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.431 ns |  1.4923 ns | 0.0818 ns |  1.16 |    0.03 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.259 ns | 10.7347 ns | 0.5884 ns |  1.50 |    0.10 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 30.073 ns | 14.5001 ns | 0.7948 ns |  5.45 |    0.17 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 36.219 ns |  9.3635 ns | 0.5132 ns |  6.56 |    0.16 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.524 ns |  2.4172 ns | 0.1325 ns |  1.00 |    0.03 |         - |          NA |
