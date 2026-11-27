```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     |  7.947 ns |  0.0620 ns | 0.0549 ns |  1.45 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     |  8.873 ns |  0.0540 ns | 0.0505 ns |  1.62 |    0.02 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  6.176 ns |  0.0644 ns | 0.0537 ns |  1.13 |    0.02 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     |  7.251 ns |  0.0644 ns | 0.0570 ns |  1.33 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 29.117 ns |  0.3330 ns | 0.2781 ns |  5.33 |    0.08 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 34.872 ns |  0.2642 ns | 0.2342 ns |  6.38 |    0.09 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  5.465 ns |  0.0813 ns | 0.0721 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  5.869 ns |  0.1319 ns | 0.1169 ns |  1.07 |    0.02 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           |  7.975 ns |  0.8540 ns | 0.0468 ns |  1.46 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           |  8.840 ns |  2.3018 ns | 0.1262 ns |  1.62 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           |  6.122 ns |  0.2621 ns | 0.0144 ns |  1.12 |    0.01 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           |  7.598 ns |  1.6426 ns | 0.0900 ns |  1.39 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 29.559 ns | 24.7826 ns | 1.3584 ns |  5.43 |    0.22 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 35.927 ns | 21.4517 ns | 1.1758 ns |  6.60 |    0.19 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  5.447 ns |  0.5749 ns | 0.0315 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  5.942 ns |  1.4008 ns | 0.0768 ns |  1.09 |    0.01 |         - |          NA |
