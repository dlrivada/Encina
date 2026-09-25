```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   642.8 ns |     2.54 ns |   2.61 ns |  0.80 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   678.3 ns |     2.01 ns |   2.15 ns |  0.85 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   801.4 ns |     4.55 ns |   4.86 ns |  1.00 |    0.01 | 0.0296 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   619.8 ns |     2.71 ns |   2.90 ns |  0.77 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   814.7 ns |     3.85 ns |   3.78 ns |  1.02 |    0.01 | 0.0334 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,314.1 ns |   150.27 ns | 147.59 ns |  5.38 |    0.18 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 7,543.5 ns |   491.01 ns | 525.37 ns |  9.41 |    0.64 | 0.1297 | 0.1221 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,156.7 ns |    17.53 ns |  20.18 ns |  2.69 |    0.03 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 7,481.1 ns |   408.02 ns | 436.58 ns |  9.34 |    0.53 | 0.1450 | 0.1373 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,130.2 ns |   140.76 ns | 150.61 ns |  3.91 |    0.18 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,262.2 ns |   263.08 ns | 292.42 ns |  4.07 |    0.36 | 0.0610 | 0.0572 |    1072 B |        2.16 |
|                               |            |                |             |             |            |             |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   648.0 ns |    95.45 ns |   5.23 ns |  0.80 |    0.03 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   729.6 ns |    25.52 ns |   1.40 ns |  0.91 |    0.03 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   805.8 ns |   527.73 ns |  28.93 ns |  1.00 |    0.04 | 0.0296 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   639.8 ns |   235.44 ns |  12.91 ns |  0.79 |    0.03 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   787.7 ns |   150.28 ns |   8.24 ns |  0.98 |    0.03 | 0.0334 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,219.1 ns |   491.10 ns |  26.92 ns |  5.24 |    0.16 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 6,646.3 ns | 3,279.01 ns | 179.73 ns |  8.26 |    0.32 | 0.1297 | 0.1221 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,177.7 ns |   203.08 ns |  11.13 ns |  2.70 |    0.08 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 6,282.8 ns | 1,820.98 ns |  99.81 ns |  7.80 |    0.26 | 0.1450 | 0.1373 |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,927.5 ns |   487.70 ns |  26.73 ns |  3.64 |    0.11 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,938.0 ns | 1,935.38 ns | 106.08 ns |  3.65 |    0.16 | 0.0610 | 0.0572 |    1072 B |        2.16 |
