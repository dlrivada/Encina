```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.444 ns | 0.0386 ns | 0.0361 ns |  1.44 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.761 ns | 0.0253 ns | 0.0224 ns |  1.60 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.343 ns | 0.0074 ns | 0.0062 ns |  1.20 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.734 ns | 0.0151 ns | 0.0118 ns |  1.36 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.817 ns | 0.0592 ns | 0.0495 ns |  5.43 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.958 ns | 0.0852 ns | 0.0665 ns |  6.60 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.624 ns | 0.0080 ns | 0.0066 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.798 ns | 0.0045 ns | 0.0040 ns |  1.14 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 13.218 ns | 0.8907 ns | 0.0488 ns |  1.54 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.780 ns | 0.3204 ns | 0.0176 ns |  1.60 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.345 ns | 0.0604 ns | 0.0033 ns |  1.20 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.996 ns | 0.6764 ns | 0.0371 ns |  1.39 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 48.127 ns | 1.6844 ns | 0.0923 ns |  5.59 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.276 ns | 0.6040 ns | 0.0331 ns |  6.65 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.609 ns | 0.1952 ns | 0.0107 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.887 ns | 2.6879 ns | 0.1473 ns |  1.15 |         - |          NA |
