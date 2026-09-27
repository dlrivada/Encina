```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.435 ns | 0.0185 ns | 0.0164 ns |  1.44 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.784 ns | 0.0227 ns | 0.0213 ns |  1.60 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.800 ns | 0.0092 ns | 0.0077 ns |  1.14 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.349 ns | 0.0136 ns | 0.0120 ns |  1.20 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.728 ns | 0.0095 ns | 0.0084 ns |  1.36 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.761 ns | 0.0118 ns | 0.0105 ns |  5.42 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.833 ns | 0.0437 ns | 0.0388 ns |  6.59 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.621 ns | 0.0049 ns | 0.0046 ns |  1.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.453 ns | 0.1227 ns | 0.0067 ns |  1.44 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.682 ns | 0.3215 ns | 0.0176 ns |  1.59 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.789 ns | 0.0525 ns | 0.0029 ns |  1.14 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.343 ns | 0.0654 ns | 0.0036 ns |  1.20 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.891 ns | 0.1193 ns | 0.0065 ns |  1.38 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.801 ns | 1.9875 ns | 0.1089 ns |  5.43 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.344 ns | 1.4536 ns | 0.0797 ns |  6.65 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.618 ns | 0.2003 ns | 0.0110 ns |  1.00 |         - |          NA |
