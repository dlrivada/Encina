```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    83.23 ns |   0.677 ns |   0.753 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    54.87 ns |   0.027 ns |   0.029 ns |  0.66 |    0.01 |      - |      - |         - |        0.00 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,904.57 ns | 304.317 ns | 325.616 ns | 46.92 |    3.83 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    61.29 ns |   0.059 ns |   0.061 ns |  0.74 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    52.48 ns |   0.053 ns |   0.061 ns |  0.63 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   124.88 ns |   1.256 ns |   1.447 ns |  1.50 |    0.02 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5,386.85 ns | 417.306 ns | 446.512 ns | 64.73 |    5.25 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,778.11 ns |  15.707 ns |  17.458 ns | 33.38 |    0.36 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 4,111.24 ns | 546.300 ns | 607.211 ns | 49.40 |    7.13 | 0.0420 | 0.0381 |     720 B |        5.00 |
|                               |            |                |             |             |             |            |            |       |         |        |        |           |             |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |    85.23 ns |   0.790 ns |   1.183 ns |  1.00 |    0.02 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |    53.46 ns |   0.088 ns |   0.129 ns |  0.63 |    0.01 |      - |      - |         - |        0.00 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          | 3,875.53 ns | 112.694 ns | 154.257 ns | 45.48 |    1.88 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |    62.49 ns |   0.662 ns |   0.991 ns |  0.73 |    0.02 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |    52.37 ns |   0.028 ns |   0.041 ns |  0.61 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |   126.21 ns |   0.876 ns |   1.311 ns |  1.48 |    0.03 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          | 5,447.03 ns | 225.152 ns | 322.907 ns | 63.92 |    3.82 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          | 2,777.81 ns |  18.998 ns |  28.435 ns | 32.60 |    0.55 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          | 4,384.62 ns | 329.800 ns | 493.630 ns | 51.46 |    5.74 | 0.0420 | 0.0381 |     720 B |        5.00 |
