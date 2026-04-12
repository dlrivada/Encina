```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev     | Median      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-----------:|-----------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    86.02 ns |   3.336 ns |   3.842 ns |    84.43 ns |  1.00 |    0.06 | 0.0057 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    55.92 ns |   0.070 ns |   0.080 ns |    55.93 ns |  0.65 |    0.03 |      - |      - |         - |        0.00 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,590.18 ns | 394.664 ns | 422.286 ns | 3,408.31 ns | 41.81 |    5.11 | 0.0267 | 0.0229 |     712 B |        4.94 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    61.14 ns |   0.038 ns |   0.039 ns |    61.13 ns |  0.71 |    0.03 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    48.84 ns |   0.106 ns |   0.122 ns |    48.82 ns |  0.57 |    0.02 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   131.15 ns |   5.429 ns |   6.252 ns |   130.14 ns |  1.53 |    0.10 | 0.0110 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,805.67 ns | 404.490 ns | 432.800 ns | 4,644.67 ns | 55.97 |    5.46 | 0.0420 | 0.0381 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,581.18 ns |  16.017 ns |  18.445 ns | 2,581.62 ns | 30.06 |    1.30 | 0.0305 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,778.45 ns | 511.425 ns | 568.447 ns | 3,821.32 ns | 44.01 |    6.72 | 0.0267 | 0.0229 |     720 B |        5.00 |
|                               |            |                |             |             |             |            |            |             |       |         |        |        |           |             |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |    80.17 ns |   1.556 ns |   2.329 ns |    80.57 ns |  1.00 |    0.04 | 0.0057 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |    50.39 ns |   0.342 ns |   0.491 ns |    50.39 ns |  0.63 |    0.02 |      - |      - |         - |        0.00 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          | 3,665.01 ns | 344.379 ns | 493.899 ns | 3,475.38 ns | 45.76 |    6.20 | 0.0267 | 0.0229 |     712 B |        4.94 |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |    57.48 ns |   1.367 ns |   2.003 ns |    55.68 ns |  0.72 |    0.03 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |    49.00 ns |   0.754 ns |   1.081 ns |    48.11 ns |  0.61 |    0.02 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |   129.71 ns |   3.179 ns |   4.758 ns |   129.50 ns |  1.62 |    0.07 | 0.0110 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          | 4,447.78 ns | 388.599 ns | 557.317 ns | 4,289.27 ns | 55.53 |    7.02 | 0.0381 | 0.0305 |    1080 B |        7.50 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          | 2,607.87 ns |  25.999 ns |  36.448 ns | 2,608.35 ns | 32.56 |    1.04 | 0.0305 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          | 4,032.61 ns | 476.926 ns | 713.841 ns | 3,949.66 ns | 50.34 |    8.89 | 0.0343 | 0.0305 |     869 B |        6.03 |
