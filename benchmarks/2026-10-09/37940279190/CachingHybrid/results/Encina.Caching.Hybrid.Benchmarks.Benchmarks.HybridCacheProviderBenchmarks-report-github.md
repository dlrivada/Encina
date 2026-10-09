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
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   684.0 ns |     4.49 ns |   4.99 ns |  0.81 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   713.9 ns |    11.69 ns |  12.50 ns |  0.84 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   849.3 ns |     9.91 ns |  10.60 ns |  1.00 |    0.02 | 0.0296 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   664.6 ns |     8.26 ns |   8.83 ns |  0.78 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   861.6 ns |     6.29 ns |   6.46 ns |  1.01 |    0.01 | 0.0334 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,865.2 ns |   140.08 ns | 143.85 ns |  5.73 |    0.18 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 8,379.7 ns |   427.15 ns | 438.65 ns |  9.87 |    0.52 | 0.1297 | 0.1221 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,303.3 ns |    21.04 ns |  22.51 ns |  2.71 |    0.04 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 7,746.4 ns |   340.60 ns | 364.43 ns |  9.12 |    0.43 | 0.1450 | 0.1373 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,474.8 ns |   170.58 ns | 182.52 ns |  4.09 |    0.21 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,709.8 ns |   235.75 ns | 262.03 ns |  4.37 |    0.31 | 0.0610 | 0.0572 |    1072 B |        2.16 |
|                               |            |                |             |             |            |             |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   692.7 ns |    53.29 ns |   2.92 ns |  0.82 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   724.1 ns |   232.57 ns |  12.75 ns |  0.85 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   849.7 ns |   319.23 ns |  17.50 ns |  1.00 |    0.03 | 0.0296 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   677.7 ns |    70.85 ns |   3.88 ns |  0.80 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   848.1 ns |    34.69 ns |   1.90 ns |  1.00 |    0.02 | 0.0334 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,525.2 ns | 1,447.31 ns |  79.33 ns |  5.33 |    0.12 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 7,098.7 ns | 4,333.15 ns | 237.51 ns |  8.36 |    0.28 | 0.1297 | 0.1221 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,288.6 ns |   190.35 ns |  10.43 ns |  2.69 |    0.05 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 7,036.5 ns | 6,083.23 ns | 333.44 ns |  8.28 |    0.37 | 0.1450 | 0.1373 |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,207.5 ns |   772.76 ns |  42.36 ns |  3.78 |    0.08 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,188.1 ns | 1,480.95 ns |  81.18 ns |  3.75 |    0.11 | 0.0610 | 0.0572 |    1072 B |        2.16 |
