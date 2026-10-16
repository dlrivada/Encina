```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    906.6 ns |     1.41 ns |     1.57 ns |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    944.9 ns |     2.54 ns |     2.83 ns |  0.85 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1,109.7 ns |     3.08 ns |     3.42 ns |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    877.0 ns |     2.30 ns |     2.56 ns |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1,136.9 ns |     3.30 ns |     3.67 ns |  1.02 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  5,721.6 ns |   155.84 ns |   166.74 ns |  5.16 |    0.15 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 10,562.5 ns | 1,351.16 ns | 1,501.81 ns |  9.52 |    1.32 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  3,375.4 ns |    43.46 ns |    50.05 ns |  3.04 |    0.04 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           |  9,522.9 ns |   336.50 ns |   345.56 ns |  8.58 |    0.30 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4,126.9 ns |   160.96 ns |   172.22 ns |  3.72 |    0.15 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4,060.1 ns |   219.66 ns |   215.73 ns |  3.66 |    0.19 | 0.0725 | 0.0687 |    1258 B |        2.54 |
|                               |            |                |             |             |             |             |             |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    929.4 ns |    18.15 ns |     0.99 ns |  0.85 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    929.6 ns |    96.26 ns |     5.28 ns |  0.85 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |  1,095.2 ns |    47.87 ns |     2.62 ns |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    902.3 ns |   161.76 ns |     8.87 ns |  0.82 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |  1,088.4 ns |    64.30 ns |     3.52 ns |  0.99 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           |  5,521.1 ns | 2,103.00 ns |   115.27 ns |  5.04 |    0.09 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           |  8,459.1 ns | 3,215.00 ns |   176.22 ns |  7.72 |    0.14 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           |  3,384.7 ns |    94.17 ns |     5.16 ns |  3.09 |    0.01 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           |  8,523.5 ns | 4,007.89 ns |   219.69 ns |  7.78 |    0.17 | 0.1831 | 0.1678 |    3643 B |        7.34 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           |  3,897.2 ns |   542.63 ns |    29.74 ns |  3.56 |    0.02 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           |  3,862.3 ns | 2,817.42 ns |   154.43 ns |  3.53 |    0.12 | 0.0610 | 0.0572 |    1072 B |        2.16 |
