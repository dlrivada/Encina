```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]     : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    882.5 ns |   7.26 ns |   8.36 ns |  0.82 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    913.3 ns |   5.93 ns |   6.83 ns |  0.85 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1,070.1 ns |   3.99 ns |   4.26 ns |  1.00 |    0.01 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    853.0 ns |   2.04 ns |   2.35 ns |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1,103.9 ns |   2.63 ns |   2.81 ns |  1.03 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  5,571.9 ns | 109.82 ns | 117.51 ns |  5.21 |    0.11 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 10,004.7 ns | 862.36 ns | 922.72 ns |  9.35 |    0.84 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  3,224.3 ns |  46.26 ns |  51.42 ns |  3.01 |    0.05 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           |  8,824.1 ns | 342.87 ns | 352.11 ns |  8.25 |    0.32 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4,036.9 ns | 151.68 ns | 162.29 ns |  3.77 |    0.15 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4,142.0 ns | 405.89 ns | 451.15 ns |  3.87 |    0.41 | 0.0610 | 0.0572 |    1072 B |        2.16 |
|                               |            |                |             |             |             |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |    902.2 ns |   2.90 ns |   4.25 ns |  0.83 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |    905.4 ns |   3.48 ns |   5.11 ns |  0.83 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |  1,089.0 ns |   4.92 ns |   7.06 ns |  1.00 |    0.01 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |    851.9 ns |   2.89 ns |   4.24 ns |  0.78 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |  1,092.4 ns |   6.34 ns |   9.10 ns |  1.00 |    0.01 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          |  5,625.3 ns | 108.86 ns | 156.13 ns |  5.17 |    0.14 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | MediumRun  | 15             | 2           | 10          | 10,352.7 ns | 558.97 ns | 801.67 ns |  9.51 |    0.73 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          |  3,326.6 ns |  41.92 ns |  61.45 ns |  3.05 |    0.06 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | MediumRun  | 15             | 2           | 10          |  8,695.4 ns | 367.41 ns | 502.91 ns |  7.98 |    0.46 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          |  4,116.9 ns |  66.54 ns |  95.42 ns |  3.78 |    0.09 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          |  4,026.2 ns | 140.28 ns | 182.41 ns |  3.70 |    0.17 | 0.0610 | 0.0572 |    1072 B |        2.16 |
