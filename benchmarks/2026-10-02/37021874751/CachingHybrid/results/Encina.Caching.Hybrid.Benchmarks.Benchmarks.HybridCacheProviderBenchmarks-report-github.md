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
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   675.7 ns |     2.78 ns |   2.98 ns |  0.87 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   675.0 ns |    14.50 ns |  16.70 ns |  0.86 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   780.7 ns |     5.73 ns |   6.36 ns |  1.00 |    0.01 | 0.0296 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   636.7 ns |    13.15 ns |  14.62 ns |  0.82 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   806.9 ns |     8.27 ns |   9.53 ns |  1.03 |    0.01 | 0.0334 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,561.1 ns |   204.77 ns | 219.10 ns |  5.84 |    0.28 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 8,477.4 ns |   554.72 ns | 593.54 ns | 10.86 |    0.74 | 0.1297 | 0.1221 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,274.0 ns |    27.00 ns |  31.10 ns |  2.91 |    0.05 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 7,285.6 ns |   284.52 ns | 304.43 ns |  9.33 |    0.39 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,258.6 ns |   159.91 ns | 171.11 ns |  4.17 |    0.22 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,575.9 ns |   346.97 ns | 385.65 ns |  4.58 |    0.48 | 0.0610 | 0.0572 |    1072 B |        2.16 |
|                               |            |                |             |             |            |             |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   659.0 ns |   211.93 ns |  11.62 ns |  0.84 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   658.3 ns |    81.22 ns |   4.45 ns |  0.84 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   780.5 ns |   121.05 ns |   6.64 ns |  1.00 |    0.01 | 0.0296 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   621.0 ns |   288.46 ns |  15.81 ns |  0.80 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   796.6 ns |   176.37 ns |   9.67 ns |  1.02 |    0.01 | 0.0334 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,669.3 ns | 3,251.81 ns | 178.24 ns |  5.98 |    0.20 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 7,422.2 ns | 4,619.75 ns | 253.22 ns |  9.51 |    0.29 | 0.1297 | 0.1221 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,364.9 ns | 1,709.93 ns |  93.73 ns |  3.03 |    0.11 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 7,380.7 ns | 4,583.14 ns | 251.22 ns |  9.46 |    0.29 | 0.1450 | 0.1373 |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,134.7 ns |   778.37 ns |  42.67 ns |  4.02 |    0.06 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,206.3 ns |    15.79 ns |   0.87 ns |  4.11 |    0.03 | 0.0610 | 0.0572 |    1072 B |        2.16 |
