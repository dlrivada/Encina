```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.457 ns |  0.0163 ns | 0.0144 ns |  1.45 |    0.00 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.766 ns |  0.0109 ns | 0.0097 ns |  1.60 |    0.00 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.336 ns |  0.0058 ns | 0.0049 ns |  1.20 |    0.00 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.719 ns |  0.0081 ns | 0.0068 ns |  1.36 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.758 ns |  0.0206 ns | 0.0183 ns |  5.42 |    0.00 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.896 ns |  0.0400 ns | 0.0355 ns |  6.60 |    0.01 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.620 ns |  0.0057 ns | 0.0047 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.795 ns |  0.0079 ns | 0.0066 ns |  1.14 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |            |           |       |         |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.434 ns |  0.1324 ns | 0.0073 ns |  1.43 |    0.02 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.684 ns |  0.2188 ns | 0.0120 ns |  1.58 |    0.02 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 11.138 ns | 18.8418 ns | 1.0328 ns |  1.28 |    0.10 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.864 ns |  0.0264 ns | 0.0014 ns |  1.37 |    0.02 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.974 ns |  6.9222 ns | 0.3794 ns |  5.41 |    0.09 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.349 ns |  3.1911 ns | 0.1749 ns |  6.60 |    0.10 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.688 ns |  2.7102 ns | 0.1486 ns |  1.00 |    0.02 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.792 ns |  0.0578 ns | 0.0032 ns |  1.13 |    0.02 |         - |          NA |
