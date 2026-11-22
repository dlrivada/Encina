```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MapAuthorizationUnauthenticated | DefaultJob | Default        | Default     | Default     | 11.358 ns | 0.0530 ns | 0.0469 ns |  1.37 |    0.01 |         - |          NA |
| MapAuthorizationForbidden       | DefaultJob | Default        | Default     | Default     | 12.815 ns | 0.1171 ns | 0.1095 ns |  1.55 |    0.01 |         - |          NA |
| MapConflictError                | DefaultJob | Default        | Default     | Default     |  9.115 ns | 0.0369 ns | 0.0345 ns |  1.10 |    0.01 |         - |          NA |
| MapUnknownError                 | DefaultJob | Default        | Default     | Default     | 10.045 ns | 0.0235 ns | 0.0219 ns |  1.21 |    0.00 |         - |          NA |
| MapMultipleValidationErrors     | DefaultJob | Default        | Default     | Default     | 41.634 ns | 0.1000 ns | 0.0835 ns |  5.03 |    0.02 |         - |          NA |
| MapMixedErrors                  | DefaultJob | Default        | Default     | Default     | 50.563 ns | 0.2600 ns | 0.2172 ns |  6.11 |    0.03 |         - |          NA |
| MapValidationError              | DefaultJob | Default        | Default     | Default     |  8.272 ns | 0.0308 ns | 0.0288 ns |  1.00 |    0.00 |         - |          NA |
| MapNotFoundError                | DefaultJob | Default        | Default     | Default     |  8.870 ns | 0.0161 ns | 0.0143 ns |  1.07 |    0.00 |         - |          NA |
|                                 |            |                |             |             |           |           |           |       |         |           |             |
| MapAuthorizationUnauthenticated | MediumRun  | 15             | 2           | 10          | 11.718 ns | 0.1976 ns | 0.2957 ns |  1.41 |    0.04 |         - |          NA |
| MapAuthorizationForbidden       | MediumRun  | 15             | 2           | 10          | 13.316 ns | 0.0452 ns | 0.0648 ns |  1.60 |    0.01 |         - |          NA |
| MapConflictError                | MediumRun  | 15             | 2           | 10          |  9.336 ns | 0.0226 ns | 0.0332 ns |  1.12 |    0.01 |         - |          NA |
| MapUnknownError                 | MediumRun  | 15             | 2           | 10          | 10.170 ns | 0.0167 ns | 0.0245 ns |  1.22 |    0.01 |         - |          NA |
| MapMultipleValidationErrors     | MediumRun  | 15             | 2           | 10          | 41.572 ns | 0.0529 ns | 0.0775 ns |  5.00 |    0.02 |         - |          NA |
| MapMixedErrors                  | MediumRun  | 15             | 2           | 10          | 49.589 ns | 0.0895 ns | 0.1340 ns |  5.97 |    0.03 |         - |          NA |
| MapValidationError              | MediumRun  | 15             | 2           | 10          |  8.312 ns | 0.0215 ns | 0.0315 ns |  1.00 |    0.01 |         - |          NA |
| MapNotFoundError                | MediumRun  | 15             | 2           | 10          |  9.141 ns | 0.0315 ns | 0.0461 ns |  1.10 |    0.01 |         - |          NA |
