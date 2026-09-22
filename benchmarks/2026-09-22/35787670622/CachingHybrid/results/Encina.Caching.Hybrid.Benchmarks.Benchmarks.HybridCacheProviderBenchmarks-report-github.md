```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   890.1 ns |      9.76 ns |  11.24 ns |  0.80 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   906.0 ns |      3.96 ns |   4.56 ns |  0.82 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           | 1,111.1 ns |      5.12 ns |   5.89 ns |  1.00 |    0.01 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   875.8 ns |      4.22 ns |   4.86 ns |  0.79 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           | 1,089.3 ns |      5.31 ns |   5.90 ns |  0.98 |    0.01 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5,454.1 ns |    145.48 ns | 155.66 ns |  4.91 |    0.14 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 9,683.7 ns |    777.56 ns | 831.98 ns |  8.72 |    0.73 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 3,295.9 ns |     26.17 ns |  30.14 ns |  2.97 |    0.03 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 8,258.9 ns |    332.91 ns | 356.21 ns |  7.43 |    0.31 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 4,090.7 ns |    139.25 ns | 149.00 ns |  3.68 |    0.13 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,877.1 ns |    202.01 ns | 198.40 ns |  3.49 |    0.17 | 0.0610 | 0.0572 |    1072 B |        2.16 |
|                               |            |                |             |             |            |              |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   881.1 ns |     13.24 ns |   0.73 ns |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   976.5 ns |    197.72 ns |  10.84 ns |  0.88 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           | 1,114.0 ns |     63.63 ns |   3.49 ns |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   874.5 ns |     83.52 ns |   4.58 ns |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           | 1,091.0 ns |     11.93 ns |   0.65 ns |  0.98 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 5,228.1 ns |  1,095.41 ns |  60.04 ns |  4.69 |    0.05 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 7,812.9 ns |  3,271.34 ns | 179.31 ns |  7.01 |    0.14 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 3,361.9 ns |    444.45 ns |  24.36 ns |  3.02 |    0.02 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 8,090.4 ns | 10,333.69 ns | 566.42 ns |  7.26 |    0.44 | 0.1678 | 0.1526 |    3044 B |        6.14 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,629.9 ns |  1,695.52 ns |  92.94 ns |  3.26 |    0.07 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,653.6 ns |    770.92 ns |  42.26 ns |  3.28 |    0.03 | 0.0610 | 0.0572 |    1072 B |        2.16 |
