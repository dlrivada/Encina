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
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   634.6 ns |     6.39 ns |   7.11 ns |  0.77 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   670.1 ns |    13.40 ns |  13.76 ns |  0.81 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   827.0 ns |     7.54 ns |   7.74 ns |  1.00 |    0.01 | 0.0296 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   645.2 ns |     7.17 ns |   7.36 ns |  0.78 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   809.1 ns |     9.22 ns |   9.86 ns |  0.98 |    0.01 | 0.0334 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,286.1 ns |   133.55 ns | 142.90 ns |  5.18 |    0.17 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 7,503.5 ns |   592.46 ns | 633.93 ns |  9.07 |    0.75 | 0.1297 | 0.1221 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,271.0 ns |    22.61 ns |  25.13 ns |  2.75 |    0.04 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 7,357.5 ns |   304.56 ns | 325.87 ns |  8.90 |    0.39 | 0.1450 | 0.1373 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,208.4 ns |   106.63 ns | 109.50 ns |  3.88 |    0.13 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,343.7 ns |   303.94 ns | 337.83 ns |  4.04 |    0.40 | 0.0610 | 0.0572 |    1072 B |        2.16 |
|                               |            |                |             |             |            |             |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   671.4 ns |    30.84 ns |   1.69 ns |  0.77 |    0.03 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   701.6 ns |    37.09 ns |   2.03 ns |  0.80 |    0.03 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   877.2 ns |   745.70 ns |  40.87 ns |  1.00 |    0.06 | 0.0296 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   663.5 ns |   403.30 ns |  22.11 ns |  0.76 |    0.04 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   825.0 ns |   770.66 ns |  42.24 ns |  0.94 |    0.06 | 0.0334 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,450.5 ns | 6,448.34 ns | 353.46 ns |  5.08 |    0.40 | 0.1068 | 0.1030 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 7,262.4 ns | 4,202.65 ns | 230.36 ns |  8.29 |    0.40 | 0.1297 | 0.1221 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,181.4 ns |   461.66 ns |  25.31 ns |  2.49 |    0.10 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 6,535.5 ns | 4,730.20 ns | 259.28 ns |  7.46 |    0.39 | 0.1450 | 0.1373 |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,014.7 ns | 2,139.60 ns | 117.28 ns |  3.44 |    0.18 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,106.6 ns | 1,345.01 ns |  73.72 ns |  3.55 |    0.16 | 0.0610 | 0.0572 |    1072 B |        2.16 |
