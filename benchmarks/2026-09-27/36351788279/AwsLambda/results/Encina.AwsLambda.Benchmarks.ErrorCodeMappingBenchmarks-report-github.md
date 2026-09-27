```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.436 ns | 0.0143 ns | 0.0111 ns |  1.44 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.772 ns | 0.0174 ns | 0.0154 ns |  1.59 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.798 ns | 0.0071 ns | 0.0063 ns |  1.13 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.345 ns | 0.0057 ns | 0.0047 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.724 ns | 0.0123 ns | 0.0115 ns |  1.36 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.734 ns | 0.0197 ns | 0.0164 ns |  5.40 |    0.00 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.829 ns | 0.0385 ns | 0.0360 ns |  6.57 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.650 ns | 0.0066 ns | 0.0055 ns |  1.00 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.555 ns | 2.9083 ns | 0.1594 ns |  1.45 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.846 ns | 2.7058 ns | 0.1483 ns |  1.60 |    0.01 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.793 ns | 0.0022 ns | 0.0001 ns |  1.13 |    0.00 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.376 ns | 0.2939 ns | 0.0161 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.855 ns | 0.0953 ns | 0.0052 ns |  1.37 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.849 ns | 2.5228 ns | 0.1383 ns |  5.41 |    0.01 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.261 ns | 0.2395 ns | 0.0131 ns |  6.61 |    0.00 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.660 ns | 0.0167 ns | 0.0009 ns |  1.00 |    0.00 |         - |          NA |
