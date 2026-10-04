```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev     | Median      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-----------:|-----------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    52.20 ns |   0.029 ns |   0.033 ns |    52.19 ns |  0.64 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    60.31 ns |   0.050 ns |   0.056 ns |    60.31 ns |  0.74 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    81.66 ns |   0.362 ns |   0.356 ns |    81.64 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    54.86 ns |   0.024 ns |   0.026 ns |    54.86 ns |  0.67 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   124.14 ns |   0.416 ns |   0.445 ns |   124.18 ns |  1.52 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5,183.84 ns | 380.546 ns | 390.792 ns | 5,161.47 ns | 63.48 |    4.66 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,894.44 ns |  23.657 ns |  27.243 ns | 2,895.64 ns | 35.45 |    0.36 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 4,095.56 ns | 501.970 ns | 557.938 ns | 3,998.24 ns | 50.16 |    6.66 | 0.0420 | 0.0381 |     720 B |        5.00 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,880.09 ns | 289.039 ns | 309.269 ns | 3,806.92 ns | 47.52 |    3.69 | 0.0420 | 0.0381 |     712 B |        4.94 |
|                               |            |                |             |             |             |            |            |             |       |         |        |        |           |             |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |    52.30 ns |   0.073 ns |   0.098 ns |    52.31 ns |  0.64 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |    60.04 ns |   0.064 ns |   0.091 ns |    60.01 ns |  0.74 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |    81.34 ns |   0.396 ns |   0.592 ns |    81.11 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |    54.69 ns |   0.965 ns |   1.353 ns |    53.48 ns |  0.67 |    0.02 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |   124.17 ns |   0.364 ns |   0.510 ns |   124.09 ns |  1.53 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          | 5,426.09 ns | 249.636 ns | 358.021 ns | 5,229.46 ns | 66.71 |    4.35 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          | 2,895.83 ns |  24.063 ns |  36.017 ns | 2,892.42 ns | 35.60 |    0.50 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          | 4,287.31 ns | 435.864 ns | 652.381 ns | 4,117.92 ns | 52.71 |    7.90 | 0.0420 | 0.0381 |     720 B |        5.00 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          | 3,825.40 ns | 151.990 ns | 213.069 ns | 3,752.17 ns | 47.03 |    2.59 | 0.0420 | 0.0381 |     712 B |        4.94 |
