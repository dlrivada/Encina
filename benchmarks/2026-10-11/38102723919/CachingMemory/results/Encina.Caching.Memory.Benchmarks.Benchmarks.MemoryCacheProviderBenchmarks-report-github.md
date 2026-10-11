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
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    52.26 ns |   0.038 ns |   0.041 ns |    52.25 ns |  0.61 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    60.07 ns |   0.063 ns |   0.064 ns |    60.06 ns |  0.70 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    85.40 ns |   1.139 ns |   1.311 ns |    85.59 ns |  1.00 |    0.02 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    53.25 ns |   0.204 ns |   0.209 ns |    53.13 ns |  0.62 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   129.57 ns |   0.882 ns |   0.944 ns |   129.35 ns |  1.52 |    0.03 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5,151.38 ns | 481.255 ns | 514.937 ns | 5,010.99 ns | 60.33 |    5.94 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,925.88 ns |  30.365 ns |  34.968 ns | 2,915.87 ns | 34.27 |    0.66 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 4,432.35 ns | 664.756 ns | 738.875 ns | 4,230.13 ns | 51.91 |    8.47 | 0.0420 | 0.0381 |     720 B |        5.00 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,964.25 ns | 319.768 ns | 342.148 ns | 3,895.74 ns | 46.43 |    3.96 | 0.0420 | 0.0381 |     712 B |        4.94 |
|                               |            |                |             |             |             |            |            |             |       |         |        |        |           |             |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |    52.28 ns |   0.056 ns |   0.075 ns |    52.27 ns |  0.62 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |    61.16 ns |   0.807 ns |   1.105 ns |    62.04 ns |  0.72 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |    84.67 ns |   0.477 ns |   0.699 ns |    84.68 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |    53.17 ns |   0.060 ns |   0.082 ns |    53.16 ns |  0.63 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |   129.87 ns |   1.695 ns |   2.376 ns |   129.52 ns |  1.53 |    0.03 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          | 5,392.77 ns | 297.103 ns | 426.096 ns | 5,230.34 ns | 63.70 |    4.97 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          | 2,908.09 ns |  24.048 ns |  35.994 ns | 2,901.52 ns | 34.35 |    0.50 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          | 4,289.95 ns | 515.382 ns | 755.440 ns | 4,148.54 ns | 50.67 |    8.78 | 0.0420 | 0.0381 |     720 B |        5.00 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          | 3,850.44 ns | 165.774 ns | 232.392 ns | 3,773.43 ns | 45.48 |    2.72 | 0.0420 | 0.0381 |     712 B |        4.94 |
