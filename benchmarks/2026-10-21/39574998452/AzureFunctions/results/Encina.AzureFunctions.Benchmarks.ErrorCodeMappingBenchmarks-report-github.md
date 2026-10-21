```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.891 ns |  0.1938 ns | 0.1813 ns |  1.51 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 10.452 ns |  0.1928 ns | 0.1803 ns |  1.77 |    0.05 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.440 ns |  0.0984 ns | 0.0873 ns |  1.09 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.402 ns |  0.0538 ns | 0.0449 ns |  1.26 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 33.456 ns |  0.3973 ns | 0.3522 ns |  5.68 |    0.13 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 38.381 ns |  0.6077 ns | 0.5684 ns |  6.52 |    0.17 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.892 ns |  0.1232 ns | 0.1265 ns |  1.00 |    0.03 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.327 ns |  0.0698 ns | 0.0583 ns |  1.07 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.923 ns |  4.1204 ns | 0.2259 ns |  1.48 |    0.05 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 10.305 ns |  1.7629 ns | 0.0966 ns |  1.71 |    0.05 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.518 ns |  3.5507 ns | 0.1946 ns |  1.08 |    0.04 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.460 ns |  0.9152 ns | 0.0502 ns |  1.24 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 32.997 ns |  8.1324 ns | 0.4458 ns |  5.48 |    0.16 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 38.315 ns | 10.8093 ns | 0.5925 ns |  6.36 |    0.19 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  6.029 ns |  3.4178 ns | 0.1873 ns |  1.00 |    0.04 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.309 ns |  0.5876 ns | 0.0322 ns |  1.05 |    0.03 |         - |          NA |
