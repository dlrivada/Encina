```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  8.325 ns |  0.1305 ns | 0.1157 ns |  1.46 |    0.03 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  9.313 ns |  0.1746 ns | 0.1458 ns |  1.63 |    0.04 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  6.055 ns |  0.1147 ns | 0.1073 ns |  1.06 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.516 ns |  0.1476 ns | 0.1309 ns |  1.14 |    0.03 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.495 ns |  0.1275 ns | 0.1130 ns |  1.32 |    0.03 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 30.944 ns |  0.5583 ns | 0.4662 ns |  5.43 |    0.12 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 37.929 ns |  0.6301 ns | 0.5586 ns |  6.66 |    0.14 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.700 ns |  0.1056 ns | 0.0936 ns |  1.00 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  8.399 ns |  3.9403 ns | 0.2160 ns |  1.46 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  9.579 ns |  5.0715 ns | 0.2780 ns |  1.66 |    0.05 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  6.129 ns |  1.0613 ns | 0.0582 ns |  1.06 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.616 ns |  0.2696 ns | 0.0148 ns |  1.15 |    0.02 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  8.271 ns |  7.8284 ns | 0.4291 ns |  1.44 |    0.07 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 30.701 ns |  4.8394 ns | 0.2653 ns |  5.33 |    0.11 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 42.656 ns | 20.9802 ns | 1.1500 ns |  7.41 |    0.23 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.757 ns |  2.3932 ns | 0.1312 ns |  1.00 |    0.03 |         - |          NA |
