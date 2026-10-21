```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.454 ns | 0.0264 ns | 0.0247 ns |  1.44 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.769 ns | 0.0097 ns | 0.0076 ns |  1.59 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.808 ns | 0.0119 ns | 0.0099 ns |  1.14 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.352 ns | 0.0043 ns | 0.0038 ns |  1.20 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.727 ns | 0.0102 ns | 0.0091 ns |  1.36 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.798 ns | 0.0154 ns | 0.0128 ns |  5.42 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.819 ns | 0.0312 ns | 0.0292 ns |  6.58 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.640 ns | 0.0150 ns | 0.0133 ns |  1.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.453 ns | 0.2766 ns | 0.0152 ns |  1.43 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.785 ns | 0.1629 ns | 0.0089 ns |  1.59 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.835 ns | 0.1609 ns | 0.0088 ns |  1.13 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.355 ns | 0.1191 ns | 0.0065 ns |  1.19 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.869 ns | 0.5952 ns | 0.0326 ns |  1.37 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.997 ns | 0.6255 ns | 0.0343 ns |  5.41 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.357 ns | 0.9548 ns | 0.0523 ns |  6.60 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.689 ns | 0.0974 ns | 0.0053 ns |  1.00 |         - |          NA |
