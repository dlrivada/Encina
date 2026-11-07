```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 12.433 ns | 0.0146 ns | 0.0130 ns |  1.44 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 13.775 ns | 0.0164 ns | 0.0145 ns |  1.60 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     | 10.338 ns | 0.0073 ns | 0.0065 ns |  1.20 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 11.742 ns | 0.0154 ns | 0.0137 ns |  1.36 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 46.809 ns | 0.0263 ns | 0.0205 ns |  5.43 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 56.840 ns | 0.0239 ns | 0.0200 ns |  6.59 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.624 ns | 0.0085 ns | 0.0075 ns |  1.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  9.803 ns | 0.0097 ns | 0.0091 ns |  1.14 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |           |             |
| MapAuthorizationUnauthenticated | ShortRun   | 3              | 1           | 3           | 12.461 ns | 0.4841 ns | 0.0265 ns |  1.44 |         - |          NA |
| MapAuthorizationForbidden       | ShortRun   | 3              | 1           | 3           | 13.801 ns | 0.3479 ns | 0.0191 ns |  1.60 |         - |          NA |
| MapConflictError                | ShortRun   | 3              | 1           | 3           | 10.351 ns | 0.0483 ns | 0.0026 ns |  1.20 |         - |          NA |
| MapUnknownError                 | ShortRun   | 3              | 1           | 3           | 11.868 ns | 0.1593 ns | 0.0087 ns |  1.37 |         - |          NA |
| MapMultipleValidationErrors     | ShortRun   | 3              | 1           | 3           | 46.831 ns | 0.1615 ns | 0.0089 ns |  5.42 |         - |          NA |
| MapMixedErrors                  | ShortRun   | 3              | 1           | 3           | 57.390 ns | 1.2957 ns | 0.0710 ns |  6.64 |         - |          NA |
| MapValidationError              | ShortRun   | 3              | 1           | 3           |  8.638 ns | 0.1327 ns | 0.0073 ns |  1.00 |         - |          NA |
| MapNotFoundError                | ShortRun   | 3              | 1           | 3           |  9.790 ns | 0.0299 ns | 0.0016 ns |  1.13 |         - |          NA |
