```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.472 ns | 0.0574 ns | 0.0509 ns |  1.44 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.761 ns | 0.0138 ns | 0.0123 ns |  1.58 |    0.01 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.809 ns | 0.0084 ns | 0.0074 ns |  1.13 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.366 ns | 0.0244 ns | 0.0204 ns |  1.19 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.738 ns | 0.0182 ns | 0.0152 ns |  1.35 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.865 ns | 0.1174 ns | 0.0981 ns |  5.39 |    0.04 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.836 ns | 0.0478 ns | 0.0373 ns |  6.54 |    0.04 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.689 ns | 0.0659 ns | 0.0617 ns |  1.00 |    0.01 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.511 ns | 1.4230 ns | 0.0780 ns |  1.44 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.739 ns | 1.4921 ns | 0.0818 ns |  1.58 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.814 ns | 0.2804 ns | 0.0154 ns |  1.13 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.361 ns | 0.3731 ns | 0.0205 ns |  1.19 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.890 ns | 1.0079 ns | 0.0552 ns |  1.37 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 47.094 ns | 6.1204 ns | 0.3355 ns |  5.42 |    0.04 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.387 ns | 1.4733 ns | 0.0808 ns |  6.61 |    0.02 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.682 ns | 0.3892 ns | 0.0213 ns |  1.00 |    0.00 |         - |          NA |
