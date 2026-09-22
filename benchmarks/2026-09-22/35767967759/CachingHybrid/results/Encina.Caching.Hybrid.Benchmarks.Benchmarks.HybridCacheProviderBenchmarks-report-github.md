```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Median     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   636.1 ns |     4.66 ns |   5.36 ns |   634.5 ns |  0.80 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   681.9 ns |     7.56 ns |   8.40 ns |   681.5 ns |  0.86 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   795.3 ns |    12.62 ns |  14.54 ns |   796.5 ns |  1.00 |    0.03 | 0.0296 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   629.1 ns |     6.68 ns |   7.69 ns |   626.3 ns |  0.79 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   792.6 ns |     8.77 ns |  10.10 ns |   794.4 ns |  1.00 |    0.02 | 0.0334 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,542.1 ns |   140.95 ns | 144.75 ns | 4,509.0 ns |  5.71 |    0.20 | 0.1068 | 0.1030 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 7,799.1 ns |   463.49 ns | 495.93 ns | 7,685.8 ns |  9.81 |    0.63 | 0.1297 | 0.1221 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,184.0 ns |    14.58 ns |  15.60 ns | 2,183.0 ns |  2.75 |    0.05 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 7,444.2 ns |   330.59 ns | 353.73 ns | 7,474.0 ns |  9.36 |    0.46 | 0.1450 | 0.1373 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,286.1 ns |   122.72 ns | 126.03 ns | 3,266.9 ns |  4.13 |    0.17 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,208.0 ns |   311.39 ns | 346.11 ns | 2,973.5 ns |  4.04 |    0.43 | 0.0610 | 0.0572 |    1072 B |        2.16 |
|                               |            |                |             |             |            |             |           |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   679.6 ns |    26.42 ns |   1.45 ns |   680.1 ns |  0.85 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   668.9 ns |    78.82 ns |   4.32 ns |   667.8 ns |  0.84 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   798.3 ns |   404.60 ns |  22.18 ns |   799.2 ns |  1.00 |    0.03 | 0.0296 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   607.4 ns |    65.01 ns |   3.56 ns |   605.9 ns |  0.76 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   771.9 ns |   108.39 ns |   5.94 ns |   771.4 ns |  0.97 |    0.02 | 0.0334 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,176.3 ns | 2,109.01 ns | 115.60 ns | 4,208.6 ns |  5.23 |    0.18 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 6,967.1 ns | 6,611.13 ns | 362.38 ns | 6,810.8 ns |  8.73 |    0.45 | 0.1297 | 0.1221 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,141.2 ns |   390.27 ns |  21.39 ns | 2,144.9 ns |  2.68 |    0.07 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 6,587.7 ns | 4,380.14 ns | 240.09 ns | 6,521.4 ns |  8.26 |    0.33 | 0.1450 | 0.1373 |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,843.8 ns |   820.12 ns |  44.95 ns | 2,820.3 ns |  3.56 |    0.10 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,850.6 ns | 1,443.89 ns |  79.14 ns | 2,809.7 ns |  3.57 |    0.12 | 0.0610 | 0.0572 |    1072 B |        2.16 |
