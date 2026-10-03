```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 10.460 ns |  0.0171 ns | 0.0151 ns | 10.456 ns |  1.38 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 11.660 ns |  0.1428 ns | 0.1115 ns | 11.701 ns |  1.54 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  8.405 ns |  0.0186 ns | 0.0174 ns |  8.405 ns |  1.11 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  9.444 ns |  0.2157 ns | 0.3359 ns |  9.239 ns |  1.25 |    0.04 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 38.290 ns |  0.1633 ns | 0.1363 ns | 38.238 ns |  5.06 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 47.394 ns |  0.3494 ns | 0.3268 ns | 47.534 ns |  6.26 |    0.04 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  7.566 ns |  0.0182 ns | 0.0142 ns |  7.566 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.225 ns |  0.1238 ns | 0.1158 ns |  8.185 ns |  1.09 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |            |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 10.555 ns |  0.1170 ns | 0.0064 ns | 10.556 ns |  1.41 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 12.322 ns | 10.5284 ns | 0.5771 ns | 12.619 ns |  1.64 |    0.07 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  8.960 ns |  5.9980 ns | 0.3288 ns |  8.821 ns |  1.19 |    0.04 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  9.340 ns |  0.2980 ns | 0.0163 ns |  9.345 ns |  1.24 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 38.265 ns |  0.2037 ns | 0.0112 ns | 38.270 ns |  5.10 |    0.02 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 47.893 ns | 25.4905 ns | 1.3972 ns | 47.173 ns |  6.38 |    0.16 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  7.505 ns |  0.7082 ns | 0.0388 ns |  7.489 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  8.399 ns |  0.1437 ns | 0.0079 ns |  8.400 ns |  1.12 |    0.01 |         - |          NA |
