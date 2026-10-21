```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   788.1 ns |     8.15 ns |   9.06 ns |  0.81 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   858.1 ns |    20.81 ns |  23.96 ns |  0.88 |    0.03 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   973.6 ns |    16.73 ns |  17.90 ns |  1.00 |    0.03 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   791.2 ns |    10.16 ns |  10.87 ns |  0.81 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   872.3 ns |    13.97 ns |  15.53 ns |  0.90 |    0.02 | 0.0334 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,844.3 ns |   207.41 ns | 221.93 ns |  4.98 |    0.24 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 8,388.4 ns |   633.35 ns | 677.68 ns |  8.62 |    0.70 | 0.1297 | 0.1221 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,592.7 ns |    21.81 ns |  25.12 ns |  2.66 |    0.05 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 7,117.8 ns |   236.93 ns | 243.31 ns |  7.31 |    0.28 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,436.7 ns |   232.49 ns | 248.77 ns |  3.53 |    0.26 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,775.6 ns |   316.90 ns | 352.23 ns |  3.88 |    0.36 | 0.0610 | 0.0572 |    1072 B |        2.16 |
|                               |            |                |             |             |            |             |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   792.7 ns |   335.52 ns |  18.39 ns |  0.82 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   879.8 ns |   360.34 ns |  19.75 ns |  0.91 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   970.3 ns |   206.05 ns |  11.29 ns |  1.00 |    0.01 | 0.0296 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   810.0 ns |   412.13 ns |  22.59 ns |  0.83 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   876.6 ns |    49.23 ns |   2.70 ns |  0.90 |    0.01 | 0.0334 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,445.3 ns | 3,006.49 ns | 164.80 ns |  4.58 |    0.15 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 7,597.2 ns | 6,522.54 ns | 357.52 ns |  7.83 |    0.33 | 0.1297 | 0.1221 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,576.7 ns |   120.59 ns |   6.61 ns |  2.66 |    0.03 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 6,594.6 ns | 2,322.81 ns | 127.32 ns |  6.80 |    0.13 | 0.1450 | 0.1373 |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,415.0 ns | 1,744.02 ns |  95.60 ns |  3.52 |    0.09 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,207.2 ns | 1,924.71 ns | 105.50 ns |  3.31 |    0.10 | 0.0610 | 0.0572 |    1072 B |        2.16 |
